using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Bookings.ListMyBookings;

namespace RoomBooking.Application.Agent.Tools;

public sealed class ListMyBookingsTool(
    ListMyBookingsUseCase useCase,
    IBusinessTimeZone businessTimeZone) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        AgentToolNames.ListMyBookings,
        "Lists active upcoming bookings owned by the authenticated user.",
        """
        {
          "type": "object",
          "properties": {},
          "additionalProperties": false
        }
        """,
        false);

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (!AgentToolJson.TryDeserialize<EmptyArguments>(argumentsJson, out _))
        {
            return AgentToolJson.Failure(
                "tool.invalid_arguments",
                "list_my_bookings received invalid arguments.");
        }

        var result = await useCase.ExecuteAsync(cancellationToken);

        return result.IsSuccess
            ? AgentToolJson.Success(
                result.Value.Select(booking => new BookingListItemToolResponse(
                    booking.Id,
                    booking.RoomCode,
                    booking.Title,
                    booking.Attendees,
                    BusinessLocalTimeConverter.ConvertToLocal(
                        booking.StartUtc,
                        businessTimeZone),
                    BusinessLocalTimeConverter.ConvertToLocal(
                        booking.EndUtc,
                        businessTimeZone))).ToArray())
            : AgentToolJson.Failure(result.Error!);
    }

    private sealed record EmptyArguments;

    private sealed record BookingListItemToolResponse(
        Guid Id,
        string RoomCode,
        string Title,
        int Attendees,
        DateTime StartLocal,
        DateTime EndLocal);
}
