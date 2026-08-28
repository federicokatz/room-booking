using RoomBooking.Domain.Common;

namespace RoomBooking.Application.Bookings;

public static class BookingApplicationErrors
{
    public static DomainError NotAuthenticated { get; } = new(
        "authentication.required",
        "An authenticated user is required.");

    public static DomainError RoomNotFound { get; } = new(
        "room.not_found",
        "The requested room was not found.");

    public static DomainError BookingNotFound { get; } = new(
        "booking.not_found",
        "The requested booking was not found.");

    public static DomainError StartMustBeInFuture { get; } = new(
        "booking.start_must_be_in_future",
        "Booking start time must be in the future.");

    public static DomainError RangeStartMustBeUtc { get; } = new(
        "schedule.range.start_must_be_utc",
        "Schedule start must be expressed in UTC.");

    public static DomainError RangeEndMustBeUtc { get; } = new(
        "schedule.range.end_must_be_utc",
        "Schedule end must be expressed in UTC.");

    public static DomainError RangeStartMustAlignToSlot { get; } = new(
        "schedule.range.start_must_align_to_slot",
        "Schedule start must align to a 30-minute boundary.");

    public static DomainError RangeEndMustAlignToSlot { get; } = new(
        "schedule.range.end_must_align_to_slot",
        "Schedule end must align to a 30-minute boundary.");

    public static DomainError RangeDurationMustBePositive { get; } = new(
        "schedule.range.duration_must_be_positive",
        "Schedule end must be later than schedule start.");
}
