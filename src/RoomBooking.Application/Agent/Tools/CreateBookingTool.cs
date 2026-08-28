using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Bookings.CreateBooking;

namespace RoomBooking.Application.Agent.Tools;

public sealed class CreateBookingTool(
    CreateBookingUseCase useCase,
    IBusinessTimeZone businessTimeZone) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        AgentToolNames.CreateBooking,
        "Creates a meeting-room booking for the authenticated user. Call only when the user explicitly asks to book and every required field is known.",
        """
        {
          "type": "object",
          "properties": {
            "roomCode": { "type": "string", "enum": ["A", "B", "C", "D", "E"] },
            "startLocal": { "type": "string", "format": "date-time", "description": "Business-local start time without an offset, aligned to a 30-minute boundary." },
            "endLocal": { "type": "string", "format": "date-time", "description": "Business-local end time without an offset, aligned to a 30-minute boundary." },
            "title": { "type": "string" },
            "attendees": { "type": "integer", "minimum": 1 }
          },
          "required": ["roomCode", "startLocal", "endLocal", "title", "attendees"],
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

        if (!BusinessLocalTimeConverter.TryConvertToUtc(
                arguments.StartLocal,
                arguments.EndLocal,
                businessTimeZone,
                out var startUtc,
                out var endUtc))
        {
            return AgentToolJson.Failure(
                "tool.invalid_arguments",
                "create_booking requires unambiguous business-local date-times without an offset.");
        }

        var result = await useCase.ExecuteAsync(
            new CreateBookingCommand(
                arguments.RoomCode,
                startUtc,
                endUtc,
                arguments.Title,
                arguments.Attendees),
            cancellationToken);

        return result.IsSuccess
            ? AgentToolJson.Success(
                BookingToolResponse.From(result.Value, businessTimeZone),
                AgentEffects.BookingCreated)
            : AgentToolJson.Failure(result.Error!);
    }

    private sealed record CreateBookingArguments(
        string? RoomCode,
        DateTime StartLocal,
        DateTime EndLocal,
        string? Title,
        int Attendees);
}
