namespace RoomBooking.Application.Tests.Fakes;

internal sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        return utcNow;
    }
}
