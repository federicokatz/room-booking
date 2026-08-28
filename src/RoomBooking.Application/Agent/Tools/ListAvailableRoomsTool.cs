using RoomBooking.Application.Bookings.ListAvailableRooms;

namespace RoomBooking.Application.Agent.Tools;

public sealed class ListAvailableRoomsTool(ListAvailableRoomsUseCase useCase) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        AgentToolNames.ListAvailableRooms,
        "Lists rooms that can host the requested attendees and are free for the complete UTC time range.",
        """
        {
          "type": "object",
          "properties": {
            "startUtc": { "type": "string", "format": "date-time", "description": "UTC start aligned to a 30-minute boundary." },
            "endUtc": { "type": "string", "format": "date-time", "description": "UTC end aligned to a 30-minute boundary." },
            "attendees": { "type": "integer", "minimum": 1 }
          },
          "required": ["startUtc", "endUtc", "attendees"],
          "additionalProperties": false
        }
        """,
        false);

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (!AgentToolJson.TryDeserialize<ListAvailableRoomsArguments>(argumentsJson, out var arguments))
        {
            return AgentToolJson.Failure(
                "tool.invalid_arguments",
                "list_available_rooms received invalid arguments.");
        }

        var result = await useCase.ExecuteAsync(
            new ListAvailableRoomsQuery(
                arguments.StartUtc,
                arguments.EndUtc,
                arguments.Attendees),
            cancellationToken);

        return result.IsSuccess
            ? AgentToolJson.Success(result.Value)
            : AgentToolJson.Failure(result.Error!);
    }

    private sealed record ListAvailableRoomsArguments(
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        int Attendees);
}
