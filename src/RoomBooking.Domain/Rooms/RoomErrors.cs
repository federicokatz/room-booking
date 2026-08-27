using RoomBooking.Domain.Common;

namespace RoomBooking.Domain.Rooms;

public static class RoomErrors
{
    public static DomainError IdRequired { get; } = new(
        "room.id_required",
        "A room identifier is required.");

    public static DomainError InvalidCode { get; } = new(
        "room.invalid_code",
        "Room code must be one of A, B, C, D, or E.");

    public static DomainError CapacityMustBePositive { get; } = new(
        "room.capacity_must_be_positive",
        "Room capacity must be greater than zero.");
}
