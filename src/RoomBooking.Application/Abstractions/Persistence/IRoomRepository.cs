using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Abstractions.Persistence;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Room?> GetByCodeAsync(RoomCode code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default);
}
