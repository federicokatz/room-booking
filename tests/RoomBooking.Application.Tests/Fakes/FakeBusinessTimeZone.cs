using RoomBooking.Application.Abstractions.Time;

namespace RoomBooking.Application.Tests.Fakes;

internal sealed class FakeBusinessTimeZone : IBusinessTimeZone
{
    public TimeZoneInfo Value { get; } = TimeZoneInfo.Utc;
}
