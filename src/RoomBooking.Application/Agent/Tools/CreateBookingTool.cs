using RoomBooking.Application.Bookings.CreateBooking;

namespace RoomBooking.Application.Agent.Tools;

public sealed class CreateBookingTool(CreateBookingUseCase useCase) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        AgentToolNames.CreateBooking,
        "Creates a meeting-room booking for the authenticated user. Call only when the user explicitly asks to book and every required field is known.",
        """
        {
          "type": "object",
          "properties": {
            "roomCode": { "type": "string", "enum": ["A", "B", "C", "D", "E"] },
            "startUtc": { "type": "string", "format": "date-time", "description": "UTC start aligned to a 30-minute boundary." },
            "endUtc": { "type": "string", "format": "date-time", "description": "UTC end aligned to a 30-minute boundary." },
            "title": { "type": "string" },
            "attendees": { "type": "integer", "minimum": 1 }
          },
          "required": ["roomCode", "startUtc", "endUtc", "title", "attendees"],
          "additionalProperties": false
        }
        """,
        true);

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (!AgentToolJson.TryDeserialize<CreateBookingArguments>(argumentsJson, out var arguments))
        {
            return AgentToolJson.Failure(
                "tool.invalid_arguments",
                "create_booking received invalid arguments.");
        }

        var result = await useCase.ExecuteAsync(
            new CreateBookingCommand(
                arguments.RoomCode,
                arguments.StartUtc,
                arguments.EndUtc,
                arguments.Title,
                arguments.Attendees),
            cancellationToken);

        return result.IsSuccess
            ? AgentToolJson.Success(
                BookingToolResponse.From(result.Value),
                AgentEffects.BookingCreated)
            : AgentToolJson.Failure(result.Error!);
    }

    private sealed record CreateBookingArguments(
        string? RoomCode,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        string? Title,
        int Attendees);
}
