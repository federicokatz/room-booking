using RoomBooking.Application.Abstractions.Authentication;
using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Agent.Tools;
using RoomBooking.Domain.Common;

namespace RoomBooking.Application.Agent;

public sealed class ChatAgentService
{
    public const int MaximumIterations = 5;
    public const int MaximumToolCalls = 5;

    private const string PartialCompletionMessage =
        "At least one requested change was completed, but the assistant could not finish the conversation. Please refresh your bookings to verify the current state.";

    private readonly ICurrentUser currentUser;
    private readonly ChatSessionStore sessionStore;
    private readonly IChatModel chatModel;
    private readonly Dictionary<string, IAgentTool> toolsByName;
    private readonly IReadOnlyList<ChatToolDefinition> toolDefinitions;
    private readonly IBusinessTimeZone businessTimeZone;
    private readonly TimeProvider timeProvider;

    public ChatAgentService(
        ICurrentUser currentUser,
        ChatSessionStore sessionStore,
        IChatModel chatModel,
        IEnumerable<IAgentTool> tools,
        IBusinessTimeZone businessTimeZone,
        TimeProvider timeProvider)
    {
        this.currentUser = currentUser;
        this.sessionStore = sessionStore;
        this.chatModel = chatModel;
        this.businessTimeZone = businessTimeZone;
        this.timeProvider = timeProvider;

        var configuredTools = tools
            .OrderBy(tool => tool.Definition.Name, StringComparer.Ordinal)
            .ToArray();
        toolsByName = configuredTools.ToDictionary(
            tool => tool.Definition.Name,
            StringComparer.Ordinal);
        toolDefinitions = configuredTools.Select(tool => tool.Definition).ToArray();
    }

    public Result<CreateChatSessionResponse> CreateSession()
    {
        if (!TryGetCurrentUserName(out var userName))
        {
            return Result.Failure<CreateChatSessionResponse>(AgentErrors.NotAuthenticated);
        }

        var sessionId = sessionStore.Create(userName);
        return Result.Success(new CreateChatSessionResponse(sessionId));
    }

    public Result<bool> DeleteSession(string sessionId)
    {
        if (!TryGetCurrentUserName(out var userName))
        {
            return Result.Failure<bool>(AgentErrors.NotAuthenticated);
        }

        return sessionStore.Delete(sessionId, userName)
            ? Result.Success(true)
            : Result.Failure<bool>(AgentErrors.SessionNotFound);
    }

    public async Task<Result<SendChatMessageResponse>> SendMessageAsync(
        string sessionId,
        string? message,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserName(out var userName))
        {
            return Result.Failure<SendChatMessageResponse>(AgentErrors.NotAuthenticated);
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Result.Failure<SendChatMessageResponse>(AgentErrors.MessageRequired);
        }

        var sessionResult = await sessionStore.ExecuteAsync(
            sessionId,
            userName,
            history => RunAgentAsync(history, message.Trim(), cancellationToken),
            cancellationToken);

        return sessionResult.Exists
            ? sessionResult.Value!
            : Result.Failure<SendChatMessageResponse>(AgentErrors.SessionNotFound);
    }

    private async Task<Result<SendChatMessageResponse>> RunAgentAsync(
        List<ChatMessage> history,
        string userMessage,
        CancellationToken cancellationToken)
    {
        history.Add(new ChatMessage(ChatMessageRole.User, userMessage));
        var effects = new HashSet<string>(StringComparer.Ordinal);
        var toolCallCount = 0;

        for (var iteration = 0; iteration < MaximumIterations; iteration++)
        {
            ChatModelResponse modelResponse;
            try
            {
                ChatSessionStore.TrimHistory(history);
                var messages = BuildModelMessages(history);
                modelResponse = await chatModel.SendAsync(
                    messages,
                    toolDefinitions,
                    cancellationToken);
            }
            catch (ChatModelException)
            {
                return CompleteOrFailAfterProviderError(history, effects);
            }

            if (modelResponse.ToolCalls.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(modelResponse.Content))
                {
                    return Result.Failure<SendChatMessageResponse>(
                        AgentErrors.InvalidModelResponse);
                }

                var assistantMessage = modelResponse.Content.Trim();
                history.Add(new ChatMessage(ChatMessageRole.Assistant, assistantMessage));

                return Result.Success(new SendChatMessageResponse(
                    assistantMessage,
                    effects.OrderBy(effect => effect, StringComparer.Ordinal).ToArray()));
            }

            if (modelResponse.ToolCalls.Any(call =>
                    string.IsNullOrWhiteSpace(call.Id)
                    || string.IsNullOrWhiteSpace(call.Name)
                    || string.IsNullOrWhiteSpace(call.ArgumentsJson)))
            {
                return Result.Failure<SendChatMessageResponse>(
                    AgentErrors.InvalidModelResponse);
            }

            if (toolCallCount + modelResponse.ToolCalls.Count > MaximumToolCalls)
            {
                return CompleteOrFailAfterLimit(history, effects);
            }

            var historyStartIndex = history.Count;
            history.Add(new ChatMessage(
                ChatMessageRole.Assistant,
                modelResponse.Content,
                ToolCalls: modelResponse.ToolCalls));

            try
            {
                foreach (var toolCall in modelResponse.ToolCalls)
                {
                    toolCallCount++;

                    AgentToolResult toolResult;
                    if (toolsByName.TryGetValue(toolCall.Name, out var tool))
                    {
                        toolResult = await tool.ExecuteAsync(
                            toolCall.ArgumentsJson,
                            cancellationToken);
                    }
                    else
                    {
                        toolResult = AgentToolJson.Failure(
                            "tool.not_found",
                            $"The tool '{toolCall.Name}' is not available.");
                    }

                    if (!string.IsNullOrWhiteSpace(toolResult.Effect))
                    {
                        effects.Add(toolResult.Effect);
                    }

                    history.Add(new ChatMessage(
                        ChatMessageRole.Tool,
                        toolResult.ContentJson,
                        toolCall.Id));
                }
            }
            catch
            {
                history.RemoveRange(historyStartIndex, history.Count - historyStartIndex);
                throw;
            }
        }

        return CompleteOrFailAfterLimit(history, effects);
    }

    private List<ChatMessage> BuildModelMessages(List<ChatMessage> history)
    {
        var messages = new List<ChatMessage>(history.Count + 1)
        {
            new(ChatMessageRole.System, BuildSystemPrompt())
        };
        messages.AddRange(history);
        return messages;
    }

    private string BuildSystemPrompt()
    {
        var utcNow = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, businessTimeZone.Value);

        return $"""
            You are a meeting-room booking assistant for an office at Cubo Itau.
            Your only purpose is to create, inspect, list, and cancel meeting-room bookings by using the provided tools.
            Rooms are identified as A, B, C, D, and E. Bookings use contiguous 30-minute slots and can last at most 3 hours.
            Never invent availability, capacities, booking identifiers, or operation outcomes. Treat every tool result as authoritative.
            Never expose internal booking identifiers in a user-facing response. If an identifier is needed to cancel a booking, use list_my_bookings to obtain it for a tool call only.
            Never ask for or supply a user identifier. The server determines the authenticated user.
            Read tools may be called when the user's query is clear.
            Call create_booking or cancel_booking only when the user expresses an explicit intent to perform that action and all required arguments are known.
            If required information is missing or the request is ambiguous, ask a concise follow-up question without calling a mutation tool.
            Refuse requests unrelated to meeting-room booking.
            Interpret relative dates in the {businessTimeZone.Value.Id} business time zone, then send all tool date-time arguments in UTC with offset +00:00.
            Current business date and time: {localNow:yyyy-MM-dd HH:mm:ss zzz}. Current UTC time: {utcNow:O}.
            Respond in the same language as the user and describe failures accurately.
            """;
    }

    private static Result<SendChatMessageResponse> CompleteOrFailAfterProviderError(
        List<ChatMessage> history,
        HashSet<string> effects)
    {
        return effects.Count > 0
            ? CompletePartialMutation(history, effects)
            : Result.Failure<SendChatMessageResponse>(AgentErrors.ProviderUnavailable);
    }

    private static Result<SendChatMessageResponse> CompleteOrFailAfterLimit(
        List<ChatMessage> history,
        HashSet<string> effects)
    {
        return effects.Count > 0
            ? CompletePartialMutation(history, effects)
            : Result.Failure<SendChatMessageResponse>(AgentErrors.ExecutionLimitReached);
    }

    private static Result<SendChatMessageResponse> CompletePartialMutation(
        List<ChatMessage> history,
        HashSet<string> effects)
    {
        history.Add(new ChatMessage(ChatMessageRole.Assistant, PartialCompletionMessage));

        return Result.Success(new SendChatMessageResponse(
            PartialCompletionMessage,
            effects.OrderBy(effect => effect, StringComparer.Ordinal).ToArray()));
    }

    private bool TryGetCurrentUserName(out string userName)
    {
        userName = currentUser.UserName ?? string.Empty;
        return currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(userName);
    }
}
