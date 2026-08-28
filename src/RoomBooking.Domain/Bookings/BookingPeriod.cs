using RoomBooking.Domain.Common;

namespace RoomBooking.Domain.Bookings;

public sealed record BookingPeriod
{
    private BookingPeriod()
    {
    }

    private BookingPeriod(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public static TimeSpan SlotDuration { get; } = TimeSpan.FromMinutes(30);

    public static TimeSpan MaximumDuration { get; } = TimeSpan.FromHours(3);

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public TimeSpan Duration => EndUtc - StartUtc;

    public static Result<BookingPeriod> Create(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (startUtc.Offset != TimeSpan.Zero)
        {
            return Result.Failure<BookingPeriod>(BookingPeriodErrors.StartMustBeUtc);
        }

        if (endUtc.Offset != TimeSpan.Zero)
        {
            return Result.Failure<BookingPeriod>(BookingPeriodErrors.EndMustBeUtc);
        }

        if (!IsAlignedToSlot(startUtc))
        {
            return Result.Failure<BookingPeriod>(BookingPeriodErrors.StartMustAlignToSlot);
        }

        if (!IsAlignedToSlot(endUtc))
        {
            return Result.Failure<BookingPeriod>(BookingPeriodErrors.EndMustAlignToSlot);
        }

        var duration = endUtc - startUtc;

        if (duration <= TimeSpan.Zero)
        {
            return Result.Failure<BookingPeriod>(BookingPeriodErrors.DurationMustBePositive);
        }

        return duration <= MaximumDuration
            ? Result.Success(new BookingPeriod(startUtc, endUtc))
            : Result.Failure<BookingPeriod>(BookingPeriodErrors.DurationExceedsMaximum);
    }

    public bool Overlaps(BookingPeriod other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return StartUtc < other.EndUtc && other.StartUtc < EndUtc;
    }

    private static bool IsAlignedToSlot(DateTimeOffset value)
    {
        return value.TimeOfDay.Ticks % SlotDuration.Ticks == 0;
    }
}
