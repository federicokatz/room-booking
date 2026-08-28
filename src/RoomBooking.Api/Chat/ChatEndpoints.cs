using RoomBooking.Api.Security;
using RoomBooking.Application.Agent;

namespace RoomBooking.Api.Chat;

internal static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var chat = endpoints.MapGroup("/api/chat/sessions").RequireAuthorization();
        chat.MapPost("", CreateSession)
            .RequireValidAntiforgeryToken();
        chat.MapPost("/{sessionId}/messages", SendMessageAsync)
            .RequireValidAntiforgeryToken();
        chat.MapDelete("/{sessionId}", DeleteSession)
            .RequireValidAntiforgeryToken();

        return endpoints;
    }

    private static IResult CreateSession(ChatAgentService agentService)
    {
        var result = agentService.CreateSession();

        return result.IsSuccess
            ? Results.Created(
                $"/api/chat/sessions/{result.Value.SessionId}",
                result.Value)
            : ChatErrorMapper.ToProblem(result.Error!);
    }

    private static async Task<IResult> SendMessageAsync(
        string sessionId,
        SendChatMessageRequest request,
        ChatAgentService agentService,
        CancellationToken cancellationToken)
    {
        var result = await agentService.SendMessageAsync(
            sessionId,
            request.Message,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ChatErrorMapper.ToProblem(result.Error!);
    }

    private static IResult DeleteSession(
        string sessionId,
        ChatAgentService agentService)
    {
        var result = agentService.DeleteSession(sessionId);

        return result.IsSuccess
            ? Results.NoContent()
            : ChatErrorMapper.ToProblem(result.Error!);
    }

    private sealed record SendChatMessageRequest(string? Message);
}
