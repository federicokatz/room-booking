namespace RoomBooking.Application.Agent;

public enum ChatMessageRole
{
    System,
    User,
    Assistant,
    Tool
}

public sealed record ChatToolCall(
    string Id,
    string Name,
    string ArgumentsJson);

public sealed record ChatMessage(
    ChatMessageRole Role,
    string? Content,
    string? ToolCallId = null,
    IReadOnlyList<ChatToolCall>? ToolCalls = null);

public sealed record ChatToolDefinition(
    string Name,
    string Description,
    string ParametersJsonSchema,
    bool IsMutation);

public sealed record ChatModelResponse(
    string? Content,
    IReadOnlyList<ChatToolCall> ToolCalls);

public interface IChatModel
{
    Task<ChatModelResponse> SendAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

public sealed class ChatModelException : Exception
{
    public ChatModelException(string message)
        : base(message)
    {
    }

    public ChatModelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
