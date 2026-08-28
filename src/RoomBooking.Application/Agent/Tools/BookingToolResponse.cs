using RoomBooking.Application.Bookings;

namespace RoomBooking.Application.Agent.Tools;

internal sealed record BookingToolResponse(
    string RoomCode,
    string Title,
    int Attendees,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc)
{
    public static BookingToolResponse From(BookingResponse booking)
    {
        return new BookingToolResponse(
            booking.RoomCode,
            booking.Title,
            booking.Attendees,
            booking.StartUtc,
            booking.EndUtc);
    }
}
