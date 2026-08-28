using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Fakes;

internal sealed class FakeRoomRepository(IEnumerable<Room> rooms) : IRoomRepository
{
    private readonly IReadOnlyList<Room> rooms = rooms.ToArray();

    public Task<Room?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(rooms.SingleOrDefault(room => room.Id == id));
    }

    public Task<Room?> GetByCodeAsync(
        RoomCode code,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(rooms.SingleOrDefault(room => room.Code == code));
    }

    public Task<IReadOnlyList<Room>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(rooms);
    }
}
