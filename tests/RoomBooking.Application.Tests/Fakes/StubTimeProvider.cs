namespace RoomBooking.Application.Tests.Fakes;

internal sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset utcNow = utcNow;

    public override DateTimeOffset GetUtcNow()
    {
        return utcNow;
    }

    public void Advance(TimeSpan duration)
    {
        utcNow = utcNow.Add(duration);
    }
}
