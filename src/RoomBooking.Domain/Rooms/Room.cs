using RoomBooking.Domain.Common;

namespace RoomBooking.Domain.Rooms;

public sealed class Room
{
    private Room(Guid id, RoomCode code, int capacity)
    {
        Id = id;
        Code = code;
        Capacity = capacity;
    }

    public Guid Id { get; }

    public RoomCode Code { get; }

    public int Capacity { get; }

    public static Result<Room> Create(Guid id, RoomCode? code, int capacity)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<Room>(RoomErrors.IdRequired);
        }

        if (code is null)
        {
            return Result.Failure<Room>(RoomErrors.InvalidCode);
        }

        return capacity > 0
            ? Result.Success(new Room(id, code, capacity))
            : Result.Failure<Room>(RoomErrors.CapacityMustBePositive);
    }

    public bool CanHost(int attendees)
    {
        return attendees > 0 && attendees <= Capacity;
    }
}
