using RoomBooking.Application.Bookings.CancelBooking;

namespace RoomBooking.Application.Agent.Tools;

public sealed class CancelBookingTool(CancelBookingUseCase useCase) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        AgentToolNames.CancelBooking,
        "Cancels a booking owned by the authenticated user. Call only when the user explicitly asks to cancel it.",
        """
        {
          "type": "object",
          "properties": {
            "bookingId": { "type": "string", "format": "uuid" }
          },
          "required": ["bookingId"],
          "additionalProperties": false
        }
        """,
        true);

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (!AgentToolJson.TryDeserialize<CancelBookingArguments>(argumentsJson, out var arguments))
        {
            return AgentToolJson.Failure(
                "tool.invalid_arguments",
                "cancel_booking received invalid arguments.");
        }

        var result = await useCase.ExecuteAsync(arguments.BookingId, cancellationToken);

        return result.IsSuccess
            ? AgentToolJson.Success(
                BookingToolResponse.From(result.Value),
                AgentEffects.BookingCancelled)
            : AgentToolJson.Failure(result.Error!);
    }

    private sealed record CancelBookingArguments(Guid BookingId);
}
