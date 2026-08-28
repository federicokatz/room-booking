using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Bookings.ListAvailableRooms;

namespace RoomBooking.Application.Agent.Tools;

public sealed class ListAvailableRoomsTool(
    ListAvailableRoomsUseCase useCase,
    IBusinessTimeZone businessTimeZone) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        AgentToolNames.ListAvailableRooms,
        "Lists rooms that can host the requested attendees and are free for the complete business-local time range.",
        """
        {
          "type": "object",
          "properties": {
            "startLocal": { "type": "string", "format": "date-time", "description": "Business-local start time without an offset, aligned to a 30-minute boundary." },
            "endLocal": { "type": "string", "format": "date-time", "description": "Business-local end time without an offset, aligned to a 30-minute boundary." },
            "attendees": { "type": "integer", "minimum": 1 }
          },
          "required": ["startLocal", "endLocal", "attendees"],
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

        if (!BusinessLocalTimeConverter.TryConvertToUtc(
                arguments.StartLocal,
                arguments.EndLocal,
                businessTimeZone,
                out var startUtc,
                out var endUtc))
        {
            return AgentToolJson.Failure(
                "tool.invalid_arguments",
                "list_available_rooms requires unambiguous business-local date-times without an offset.");
        }

        var result = await useCase.ExecuteAsync(
            new ListAvailableRoomsQuery(
                startUtc,
                endUtc,
                arguments.Attendees),
            cancellationToken);

        return result.IsSuccess
            ? AgentToolJson.Success(result.Value)
            : AgentToolJson.Failure(result.Error!);
    }

    private sealed record ListAvailableRoomsArguments(
        DateTime StartLocal,
        DateTime EndLocal,
        int Attendees);
}
