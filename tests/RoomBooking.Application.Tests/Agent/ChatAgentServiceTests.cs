using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Agent;
using RoomBooking.Application.Agent.Tools;
using RoomBooking.Application.Tests.Fakes;

namespace RoomBooking.Application.Tests.Agent;

[TestClass]
public class ChatAgentServiceTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task MissingInformationReturnsFollowUpWithoutExecutingMutation()
    {
        var model = new FakeChatModel();
        model.Enqueue(FinalResponse("¿Cuántas personas asistirán?"));
        var createTool = new FakeAgentTool(
            AgentToolNames.CreateBooking,
            new AgentToolResult("{\"success\":true}", AgentEffects.BookingCreated),
            true);
        var service = CreateService("User1", model, [createTool]);
        var session = service.CreateSession().Value;

        var result = await service.SendMessageAsync(
            session.SessionId,
            "Reservame una sala mañana");

        result.IsSuccess.Should().BeTrue();
        result.Value.AssistantMessage.Should().Be("¿Cuántas personas asistirán?");
        result.Value.Effects.Should().BeEmpty();
        createTool.ExecutionCount.Should().Be(0);
    }

    [TestMethod]
    public async Task MultiTurnConversationExecutesToolAndReturnsEffect()
    {
        var model = new FakeChatModel();
        model.Enqueue(FinalResponse("¿Cuántas personas asistirán?"));
        model.Enqueue(ToolResponse(
            AgentToolNames.CreateBooking,
            "{\"roomCode\":\"A\",\"startLocal\":\"2026-09-02T10:00:00\",\"endLocal\":\"2026-09-02T11:00:00\",\"title\":\"Planning\",\"attendees\":3}"));
        model.Enqueue(FinalResponse("La sala A quedó reservada."));
        var createTool = new FakeAgentTool(
            AgentToolNames.CreateBooking,
            new AgentToolResult("{\"success\":true}", AgentEffects.BookingCreated),
            true);
        var service = CreateService("User1", model, [createTool]);
        var session = service.CreateSession().Value;
        await service.SendMessageAsync(session.SessionId, "Reservame la sala A mañana a las 10");

        var result = await service.SendMessageAsync(session.SessionId, "Somos 3, título Planning");

        result.IsSuccess.Should().BeTrue();
        result.Value.AssistantMessage.Should().Be("La sala A quedó reservada.");
        result.Value.Effects.Should().Equal(AgentEffects.BookingCreated);
        createTool.ExecutionCount.Should().Be(1);
        model.Requests.Should().HaveCount(3);
        model.Requests[1].Should().Contain(message =>
            message.Role == ChatMessageRole.Assistant
            && message.Content == "¿Cuántas personas asistirán?");
        model.Requests[2].Should().Contain(message =>
            message.Role == ChatMessageRole.Tool
            && message.Content == "{\"success\":true}");
    }

    [TestMethod]
    public async Task SessionCannotBeUsedByAnotherAuthenticatedUser()
    {
        var store = new ChatSessionStore(new StubTimeProvider(UtcNow));
        var user1Model = new FakeChatModel();
        var user1Service = CreateService("User1", user1Model, [], store);
        var session = user1Service.CreateSession().Value;
        var user2Service = CreateService("User2", new FakeChatModel(), [], store);

        var sendResult = await user2Service.SendMessageAsync(session.SessionId, "Hola");
        var deleteResult = user2Service.DeleteSession(session.SessionId);

        sendResult.Error.Should().Be(AgentErrors.SessionNotFound);
        deleteResult.Error.Should().Be(AgentErrors.SessionNotFound);
        user1Model.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ProviderFailureReturnsControlledErrorWithoutMutation()
    {
        var model = new FakeChatModel();
        model.EnqueueException(new ChatModelException("Provider unavailable."));
        var service = CreateService("User1", model, []);
        var session = service.CreateSession().Value;

        var result = await service.SendMessageAsync(session.SessionId, "¿Qué salas hay libres?");

        result.Error.Should().Be(AgentErrors.ProviderUnavailable);
    }

    [TestMethod]
    public async Task ProviderFailureAfterMutationReturnsEffectWithoutRetryingMutation()
    {
        var model = new FakeChatModel();
        model.Enqueue(ToolResponse(AgentToolNames.CancelBooking, "{\"bookingId\":\"f79f9561-60bc-41b5-9834-e7dcbe14db80\"}"));
        model.EnqueueException(new ChatModelException("Provider unavailable."));
        var cancelTool = new FakeAgentTool(
            AgentToolNames.CancelBooking,
            new AgentToolResult("{\"success\":true}", AgentEffects.BookingCancelled),
            true);
        var service = CreateService("User1", model, [cancelTool]);
        var session = service.CreateSession().Value;

        var result = await service.SendMessageAsync(session.SessionId, "Cancelá esa reserva");

        result.IsSuccess.Should().BeTrue();
        result.Value.Effects.Should().Equal(AgentEffects.BookingCancelled);
        result.Value.AssistantMessage.Should().Contain("At least one requested change was completed");
        cancelTool.ExecutionCount.Should().Be(1);
    }

    [TestMethod]
    public async Task RepeatedToolCallsStopAtIterationLimit()
    {
        var model = new FakeChatModel();
        for (var index = 0; index < ChatAgentService.MaximumIterations; index++)
        {
            model.Enqueue(ToolResponse("unknown_tool", "{}", $"call-{index}"));
        }

        var service = CreateService("User1", model, []);
        var session = service.CreateSession().Value;

        var result = await service.SendMessageAsync(session.SessionId, "Loop");

        result.Error.Should().Be(AgentErrors.ExecutionLimitReached);
        model.Requests.Should().HaveCount(ChatAgentService.MaximumIterations);
    }

    [TestMethod]
    public async Task ToolCallBatchExceedingLimitDoesNotExecuteToolsOrCorruptSession()
    {
        var model = new FakeChatModel();
        var calls = Enumerable.Range(1, ChatAgentService.MaximumToolCalls + 1)
            .Select(index => new ChatToolCall($"call-{index}", "read_tool", "{}"))
            .ToArray();
        model.Enqueue(new ChatModelResponse(null, calls));
        model.Enqueue(FinalResponse("La sesión sigue siendo válida."));
        var tool = new FakeAgentTool(
            "read_tool",
            new AgentToolResult("{\"success\":true}"));
        var service = CreateService("User1", model, [tool]);
        var session = service.CreateSession().Value;

        var firstResult = await service.SendMessageAsync(session.SessionId, "Consultá todo");
        var secondResult = await service.SendMessageAsync(session.SessionId, "Continuemos");

        firstResult.Error.Should().Be(AgentErrors.ExecutionLimitReached);
        secondResult.IsSuccess.Should().BeTrue();
        tool.ExecutionCount.Should().Be(0);
        model.Requests[1].Any(message =>
            message.Role == ChatMessageRole.Assistant
            && message.ToolCalls is { Count: > 0 }).Should().BeFalse();
    }

    [TestMethod]
    public async Task UnknownToolResultIsReturnedToModel()
    {
        var model = new FakeChatModel();
        model.Enqueue(ToolResponse("unknown_tool", "{}"));
        model.Enqueue(FinalResponse("No tengo esa herramienta."));
        var service = CreateService("User1", model, []);
        var session = service.CreateSession().Value;

        var result = await service.SendMessageAsync(session.SessionId, "Usá una herramienta desconocida");

        result.IsSuccess.Should().BeTrue();
        model.Requests[1].Should().Contain(message =>
            message.Role == ChatMessageRole.Tool
            && message.Content!.Contains("tool.not_found", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ToolBusinessErrorIsReturnedToModel()
    {
        var model = new FakeChatModel();
        model.Enqueue(ToolResponse("read_tool", "{}"));
        model.Enqueue(FinalResponse("La sala ya está reservada."));
        var tool = new FakeAgentTool(
            "read_tool",
            new AgentToolResult(
                "{\"success\":false,\"code\":\"booking.overlap\",\"message\":\"The room is already booked.\"}"));
        var service = CreateService("User1", model, [tool]);
        var session = service.CreateSession().Value;

        var result = await service.SendMessageAsync(session.SessionId, "Reservá esa sala");

        result.IsSuccess.Should().BeTrue();
        result.Value.AssistantMessage.Should().Be("La sala ya está reservada.");
        model.Requests[1].Should().Contain(message =>
            message.Role == ChatMessageRole.Tool
            && message.Content!.Contains("booking.overlap", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task EmptyModelResponseReturnsControlledError()
    {
        var model = new FakeChatModel();
        model.Enqueue(new ChatModelResponse(null, []));
        var service = CreateService("User1", model, []);
        var session = service.CreateSession().Value;

        var result = await service.SendMessageAsync(session.SessionId, "Hola");

        result.Error.Should().Be(AgentErrors.InvalidModelResponse);
    }

    [TestMethod]
    public async Task ToolExceptionDoesNotLeaveIncompleteToolCallBlockInSession()
    {
        var model = new FakeChatModel();
        model.Enqueue(ToolResponse("failing_tool", "{}"));
        model.Enqueue(FinalResponse("La conversación se recuperó."));
        var tool = new FakeAgentTool(
            "failing_tool",
            new AgentToolResult("{\"success\":true}"),
            exception: new InvalidOperationException("Tool failure."));
        var service = CreateService("User1", model, [tool]);
        var session = service.CreateSession().Value;

        Func<Task> firstRequest = async () => await service.SendMessageAsync(
            session.SessionId,
            "Fallá");

        await firstRequest.Should().ThrowAsync<InvalidOperationException>();

        var secondResult = await service.SendMessageAsync(session.SessionId, "Continuemos");

        secondResult.IsSuccess.Should().BeTrue();
        model.Requests[1].Any(message =>
            message.Role == ChatMessageRole.Tool
            || (message.Role == ChatMessageRole.Assistant
                && message.ToolCalls is { Count: > 0 })).Should().BeFalse();
    }

    [TestMethod]
    public async Task ExpiredSessionCannotBeUsed()
    {
        var timeProvider = new StubTimeProvider(UtcNow);
        var store = new ChatSessionStore(timeProvider);
        var model = new FakeChatModel();
        var service = new ChatAgentService(
            new FakeCurrentUser("User1"),
            store,
            model,
            [],
            new FakeBusinessTimeZone(),
            timeProvider);
        var session = service.CreateSession().Value;
        timeProvider.Advance(ChatSessionStore.SlidingExpiration);

        var result = await service.SendMessageAsync(session.SessionId, "Hola");

        result.Error.Should().Be(AgentErrors.SessionNotFound);
        model.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ModelContextKeepsBoundedHistory()
    {
        var model = new FakeChatModel();
        for (var index = 0; index < 12; index++)
        {
            model.Enqueue(FinalResponse($"Respuesta {index}"));
        }

        var service = CreateService("User1", model, []);
        var session = service.CreateSession().Value;

        for (var index = 0; index < 12; index++)
        {
            await service.SendMessageAsync(session.SessionId, $"Mensaje {index}");
        }

        model.Requests.Should().OnlyContain(request =>
            request.Count <= ChatSessionStore.MaximumHistoryMessages + 1);
    }

    [TestMethod]
    public async Task HistoryTrimmingKeepsValidToolCallingSequence()
    {
        var model = new FakeChatModel();
        for (var index = 0; index < 7; index++)
        {
            model.Enqueue(ToolResponse("read_tool", "{}", $"call-{index}"));
            model.Enqueue(FinalResponse($"Respuesta {index}"));
        }

        var tool = new FakeAgentTool(
            "read_tool",
            new AgentToolResult("{\"success\":true}"));
        var service = CreateService("User1", model, [tool]);
        var session = service.CreateSession().Value;

        for (var index = 0; index < 7; index++)
        {
            var result = await service.SendMessageAsync(session.SessionId, $"Mensaje {index}");
            result.IsSuccess.Should().BeTrue();
        }

        model.Requests.All(HasValidToolCallingSequence).Should().BeTrue();
    }

    [TestMethod]
    public async Task SystemPromptContainsScopeTimeZoneAndLanguagePolicy()
    {
        var model = new FakeChatModel();
        model.Enqueue(FinalResponse("No puedo ayudar con eso."));
        var service = CreateService("User1", model, []);
        var session = service.CreateSession().Value;

        await service.SendMessageAsync(session.SessionId, "Escribí un poema");

        var systemMessage = model.Requests.Single()[0];
        systemMessage.Role.Should().Be(ChatMessageRole.System);
        systemMessage.Content.Should().Contain("only purpose");
        systemMessage.Content.Should().Contain("UTC");
        systemMessage.Content.Should().Contain("startLocal");
        systemMessage.Content.Should().Contain("2026-09-01");
        systemMessage.Content.Should().Contain("latest message");
        systemMessage.Content.Should().Contain("exclusively in English");
        systemMessage.Content.Should().Contain("dominant language");
        systemMessage.Content.Should().Contain("business time zone");
    }

    private static ChatAgentService CreateService(
        string userName,
        FakeChatModel model,
        IReadOnlyList<IAgentTool> tools,
        ChatSessionStore? store = null)
    {
        var timeProvider = new StubTimeProvider(UtcNow);

        return new ChatAgentService(
            new FakeCurrentUser(userName),
            store ?? new ChatSessionStore(timeProvider),
            model,
            tools,
            new FakeBusinessTimeZone(),
            timeProvider);
    }

    private static ChatModelResponse FinalResponse(string content)
    {
        return new ChatModelResponse(content, []);
    }

    private static ChatModelResponse ToolResponse(
        string toolName,
        string argumentsJson,
        string callId = "call-1")
    {
        return new ChatModelResponse(
            null,
            [new ChatToolCall(callId, toolName, argumentsJson)]);
    }

    private static bool HasValidToolCallingSequence(IReadOnlyList<ChatMessage> messages)
    {
        var pendingToolCallIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var message in messages)
        {
            if (message.Role == ChatMessageRole.Assistant
                && message.ToolCalls is { Count: > 0 })
            {
                if (pendingToolCallIds.Count > 0)
                {
                    return false;
                }

                foreach (var toolCall in message.ToolCalls)
                {
                    if (!pendingToolCallIds.Add(toolCall.Id))
                    {
                        return false;
                    }
                }

                continue;
            }

            if (message.Role == ChatMessageRole.Tool)
            {
                if (string.IsNullOrWhiteSpace(message.ToolCallId)
                    || !pendingToolCallIds.Remove(message.ToolCallId))
                {
                    return false;
                }

                continue;
            }

            if (pendingToolCallIds.Count > 0)
            {
                return false;
            }
        }

        return pendingToolCallIds.Count == 0;
    }
}
