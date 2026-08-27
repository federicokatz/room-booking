using RoomBooking.Domain.Common;

namespace RoomBooking.Domain.Bookings;

public static class BookingErrors
{
    public static DomainError IdRequired { get; } = new(
        "booking.id_required",
        "A booking identifier is required.");

    public static DomainError OwnerRequired { get; } = new(
        "booking.owner_required",
        "A booking owner is required.");

    public static DomainError TitleRequired { get; } = new(
        "booking.title_required",
        "A booking title is required.");

    public static DomainError TitleTooLong { get; } = new(
        "booking.title_too_long",
        $"Booking title cannot exceed {Booking.MaxTitleLength} characters.");

    public static DomainError AttendeesMustBePositive { get; } = new(
        "booking.attendees_must_be_positive",
        "Attendee count must be greater than zero.");

    public static DomainError CapacityExceeded { get; } = new(
        "booking.capacity_exceeded",
        "Attendee count exceeds room capacity.");

    public static DomainError NotOwner { get; } = new(
        "booking.not_owner",
        "Only the booking owner can cancel it.");

    public static DomainError AlreadyCancelled { get; } = new(
        "booking.already_cancelled",
        "The booking is already cancelled.");

    public static DomainError CancellationTimeMustBeUtc { get; } = new(
        "booking.cancellation_time_must_be_utc",
        "Cancellation time must be expressed in UTC.");

    public static DomainError Overlap { get; } = new(
        "booking.overlap",
        "The room is already booked during the requested period.");
}
