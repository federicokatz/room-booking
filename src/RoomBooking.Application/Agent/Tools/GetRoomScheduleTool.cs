using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Bookings.GetRoomSchedule;

namespace RoomBooking.Application.Agent.Tools;

public sealed class GetRoomScheduleTool(
    GetRoomScheduleUseCase useCase,
    IBusinessTimeZone businessTimeZone) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        AgentToolNames.GetRoomSchedule,
        "Returns occupied and available 30-minute slots for one room over a business-local time range. It never exposes booking owners.",
        """
        {
          "type": "object",
          "properties": {
            "roomCode": { "type": "string", "enum": ["A", "B", "C", "D", "E"] },
            "startLocal": { "type": "string", "format": "date-time", "description": "Business-local range start without an offset, aligned to a 30-minute boundary." },
            "endLocal": { "type": "string", "format": "date-time", "description": "Business-local range end without an offset, aligned to a 30-minute boundary." }
          },
          "required": ["roomCode", "startLocal", "endLocal"],
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

        if (!BusinessLocalTimeConverter.TryConvertToUtc(
                arguments.StartLocal,
                arguments.EndLocal,
                businessTimeZone,
                out var startUtc,
                out var endUtc))
        {
            return AgentToolJson.Failure(
                "tool.invalid_arguments",
                "get_room_schedule requires unambiguous business-local date-times without an offset.");
        }

        var result = await useCase.ExecuteAsync(
            new GetRoomScheduleQuery(
                arguments.RoomCode,
                startUtc,
                endUtc),
            cancellationToken);

        return result.IsSuccess
            ? AgentToolJson.Success(
                RoomScheduleToolResponse.From(result.Value, businessTimeZone))
            : AgentToolJson.Failure(result.Error!);
    }

    private sealed record GetRoomScheduleArguments(
        string? RoomCode,
        DateTime StartLocal,
        DateTime EndLocal);

    private sealed record RoomScheduleToolResponse(
        string RoomCode,
        IReadOnlyList<RoomScheduleSlotToolResponse> Slots)
    {
        public static RoomScheduleToolResponse From(
            RoomScheduleResponse schedule,
            IBusinessTimeZone businessTimeZone)
        {
            return new RoomScheduleToolResponse(
                schedule.RoomCode,
                schedule.Slots.Select(slot => new RoomScheduleSlotToolResponse(
                    BusinessLocalTimeConverter.ConvertToLocal(
                        slot.StartUtc,
                        businessTimeZone),
                    BusinessLocalTimeConverter.ConvertToLocal(
                        slot.EndUtc,
                        businessTimeZone),
                    slot.IsOccupied)).ToArray());
        }
    }

    private sealed record RoomScheduleSlotToolResponse(
        DateTime StartLocal,
        DateTime EndLocal,
        bool IsOccupied);
}
