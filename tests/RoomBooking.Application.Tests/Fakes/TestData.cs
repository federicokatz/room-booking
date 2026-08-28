using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Fakes;

internal static class TestData
{
    public static Room CreateRoom(RoomCode code, int capacity)
    {
        return Room.Create(Guid.NewGuid(), code, capacity).Value;
    }

    public static Booking CreateBooking(
        Room room,
        string ownerId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string title = "Planning")
    {
        var period = BookingPeriod.Create(startUtc, endUtc).Value;

        return Booking.Create(Guid.NewGuid(), room, ownerId, title, 2, period).Value;
    }

    public static DateTimeOffset Utc(int hour, int minute = 0)
    {
        return new DateTimeOffset(2026, 9, 1, hour, minute, 0, TimeSpan.Zero);
    }
}
