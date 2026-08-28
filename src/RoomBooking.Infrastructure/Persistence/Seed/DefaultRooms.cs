using RoomBooking.Domain.Rooms;

namespace RoomBooking.Infrastructure.Persistence.Seed;

internal static class DefaultRooms
{
    public static IReadOnlyCollection<Room> All { get; } =
    [
        Create("00000000-0000-0000-0000-000000000001", RoomCode.A, 4),
        Create("00000000-0000-0000-0000-000000000002", RoomCode.B, 6),
        Create("00000000-0000-0000-0000-000000000003", RoomCode.C, 8),
        Create("00000000-0000-0000-0000-000000000004", RoomCode.D, 10),
        Create("00000000-0000-0000-0000-000000000005", RoomCode.E, 12)
    ];

    private static Room Create(string id, RoomCode code, int capacity)
    {
        return Room.Create(Guid.Parse(id), code, capacity).Value;
    }
}
