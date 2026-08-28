using RoomBooking.Application.Bookings.GetRoomSchedule;

namespace RoomBooking.Application.Agent.Tools;

public sealed class GetRoomScheduleTool(GetRoomScheduleUseCase useCase) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        AgentToolNames.GetRoomSchedule,
        "Returns occupied and available 30-minute slots for one room over a UTC time range. It never exposes booking owners.",
        """
        {
          "type": "object",
          "properties": {
            "roomCode": { "type": "string", "enum": ["A", "B", "C", "D", "E"] },
            "startUtc": { "type": "string", "format": "date-time", "description": "UTC range start aligned to a 30-minute boundary." },
            "endUtc": { "type": "string", "format": "date-time", "description": "UTC range end aligned to a 30-minute boundary." }
          },
          "required": ["roomCode", "startUtc", "endUtc"],
          "additionalProperties": false
        }
        """,
        false);

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (!AgentToolJson.TryDeserialize<GetRoomScheduleArguments>(argumentsJson, out var arguments))
        {
            return AgentToolJson.Failure(
                "tool.invalid_arguments",
                "get_room_schedule received invalid arguments.");
        }

        var result = await useCase.ExecuteAsync(
            new GetRoomScheduleQuery(
                arguments.RoomCode,
                arguments.StartUtc,
                arguments.EndUtc),
            cancellationToken);

        return result.IsSuccess
            ? AgentToolJson.Success(result.Value)
            : AgentToolJson.Failure(result.Error!);
    }

    private sealed record GetRoomScheduleArguments(
        string? RoomCode,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc);
}
