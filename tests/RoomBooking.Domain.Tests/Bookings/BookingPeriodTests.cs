using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Domain.Bookings;

namespace RoomBooking.Domain.Tests.Bookings;

[TestClass]
public class BookingPeriodTests
{
    [TestMethod]
    [DataRow(30)]
    [DataRow(60)]
    [DataRow(180)]
    public void CreateAcceptsAlignedDurationUpToThreeHours(int durationMinutes)
    {
        var start = Utc(10);

        var result = BookingPeriod.Create(start, start.AddMinutes(durationMinutes));

        result.IsSuccess.Should().BeTrue();
        result.Value.Duration.Should().Be(TimeSpan.FromMinutes(durationMinutes));
    }

    [TestMethod]
    public void CreateRejectsNonUtcStart()
    {
        var start = new DateTimeOffset(2026, 8, 28, 10, 0, 0, TimeSpan.FromHours(-3));

        var result = BookingPeriod.Create(start, Utc(14));

        result.Error.Should().Be(BookingPeriodErrors.StartMustBeUtc);
    }

    [TestMethod]
    public void CreateRejectsNonUtcEnd()
    {
        var end = new DateTimeOffset(2026, 8, 28, 11, 0, 0, TimeSpan.FromHours(-3));

        var result = BookingPeriod.Create(Utc(10), end);

        result.Error.Should().Be(BookingPeriodErrors.EndMustBeUtc);
    }

    [TestMethod]
    public void CreateRejectsMisalignedStart()
    {
        var result = BookingPeriod.Create(Utc(10, 15), Utc(11));

        result.Error.Should().Be(BookingPeriodErrors.StartMustAlignToSlot);
    }

    [TestMethod]
    public void CreateRejectsStartAtFortyFiveMinutesPastTheHour()
    {
        var result = BookingPeriod.Create(Utc(10, 45), Utc(11, 30));

        result.Error.Should().Be(BookingPeriodErrors.StartMustAlignToSlot);
    }

    [TestMethod]
    public void CreateRejectsMisalignedEnd()
    {
        var result = BookingPeriod.Create(Utc(10), Utc(11, 15));

        result.Error.Should().Be(BookingPeriodErrors.EndMustAlignToSlot);
    }

    [TestMethod]
    public void CreateRejectsEndAtFortyFiveMinutesPastTheHour()
    {
        var result = BookingPeriod.Create(Utc(10, 30), Utc(11, 45));

        result.Error.Should().Be(BookingPeriodErrors.EndMustAlignToSlot);
    }

    [TestMethod]
    public void CreateRejectsZeroDuration()
    {
        var start = Utc(10);

        var result = BookingPeriod.Create(start, start);

        result.Error.Should().Be(BookingPeriodErrors.DurationMustBePositive);
    }

    [TestMethod]
    public void CreateRejectsNegativeDuration()
    {
        var result = BookingPeriod.Create(Utc(11), Utc(10));

        result.Error.Should().Be(BookingPeriodErrors.DurationMustBePositive);
    }

    [TestMethod]
    public void CreateRejectsDurationLongerThanThreeHours()
    {
        var result = BookingPeriod.Create(Utc(10), Utc(13, 30));

        result.Error.Should().Be(BookingPeriodErrors.DurationExceedsMaximum);
    }

    [TestMethod]
    public void OverlapsReturnsTrueForIntersectingPeriods()
    {
        var first = CreatePeriod(10, 0, 11, 30);
        var second = CreatePeriod(11, 0, 12, 0);

        first.Overlaps(second).Should().BeTrue();
        second.Overlaps(first).Should().BeTrue();
    }

    [TestMethod]
    public void OverlapsReturnsFalseForAdjacentPeriods()
    {
        var first = CreatePeriod(10, 0, 11, 30);
        var second = CreatePeriod(11, 30, 12, 0);

        first.Overlaps(second).Should().BeFalse();
        second.Overlaps(first).Should().BeFalse();
    }

    private static BookingPeriod CreatePeriod(int startHour, int startMinute, int endHour, int endMinute)
    {
        return BookingPeriod.Create(Utc(startHour, startMinute), Utc(endHour, endMinute)).Value;
    }

    private static DateTimeOffset Utc(int hour, int minute = 0)
    {
        return new DateTimeOffset(2026, 8, 28, hour, minute, 0, TimeSpan.Zero);
    }
}
