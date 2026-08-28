using RoomBooking.Application.Bookings;
using RoomBooking.Application.Abstractions.Time;

namespace RoomBooking.Application.Agent.Tools;

internal sealed record BookingToolResponse(
    string RoomCode,
    string Title,
    int Attendees,
    DateTime StartLocal,
    DateTime EndLocal)
{
    public static BookingToolResponse From(
        BookingResponse booking,
        IBusinessTimeZone businessTimeZone)
    {
        return new BookingToolResponse(
            booking.RoomCode,
            booking.Title,
            booking.Attendees,
            BusinessLocalTimeConverter.ConvertToLocal(booking.StartUtc, businessTimeZone),
            BusinessLocalTimeConverter.ConvertToLocal(booking.EndUtc, businessTimeZone));
    }
}
