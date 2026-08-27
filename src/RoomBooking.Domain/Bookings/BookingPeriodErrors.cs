using RoomBooking.Domain.Common;

namespace RoomBooking.Domain.Bookings;

public static class BookingPeriodErrors
{
    public static DomainError StartMustBeUtc { get; } = new(
        "booking.period.start_must_be_utc",
        "Booking start must be expressed in UTC.");

    public static DomainError EndMustBeUtc { get; } = new(
        "booking.period.end_must_be_utc",
        "Booking end must be expressed in UTC.");

    public static DomainError StartMustAlignToSlot { get; } = new(
        "booking.period.start_must_align_to_slot",
        "Booking start must align to a 30-minute boundary.");

    public static DomainError EndMustAlignToSlot { get; } = new(
        "booking.period.end_must_align_to_slot",
        "Booking end must align to a 30-minute boundary.");

    public static DomainError DurationMustBePositive { get; } = new(
        "booking.period.duration_must_be_positive",
        "Booking end must be later than booking start.");

    public static DomainError DurationExceedsMaximum { get; } = new(
        "booking.period.duration_exceeds_maximum",
        "Booking duration cannot exceed three hours.");
}
