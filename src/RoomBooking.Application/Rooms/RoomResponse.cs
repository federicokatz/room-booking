using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Rooms;

public sealed record RoomResponse(Guid Id, string Code, int Capacity)
{
    internal static RoomResponse From(Room room)
    {
        return new RoomResponse(room.Id, room.Code.Value, room.Capacity);
    }
}
