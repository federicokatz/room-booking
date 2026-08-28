using RoomBooking.Application.Abstractions.Time;

namespace RoomBooking.Application.Tests.Fakes;

internal sealed class FakeBusinessTimeZone(TimeZoneInfo? timeZone = null) : IBusinessTimeZone
{
    public TimeZoneInfo Value { get; } = timeZone ?? TimeZoneInfo.Utc;
}
