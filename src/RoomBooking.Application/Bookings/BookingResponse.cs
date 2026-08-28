using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Bookings;

public sealed record BookingResponse(
    Guid Id,
    string RoomCode,
    string Title,
    int Attendees,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    BookingStatus Status,
    DateTimeOffset? CancelledAtUtc)
{
    internal static BookingResponse From(Booking booking, Room room)
    {
        return new BookingResponse(
            booking.Id,
            room.Code.Value,
            booking.Title,
            booking.Attendees,
            booking.Period.StartUtc,
            booking.Period.EndUtc,
            booking.Status,
            booking.CancelledAtUtc);
    }
}
