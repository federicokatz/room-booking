using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Infrastructure.Persistence.Repositories;

internal sealed class RoomRepository(RoomBookingDbContext dbContext) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Rooms
            .AsNoTracking()
            .SingleOrDefaultAsync(room => room.Id == id, cancellationToken);
    }

    public Task<Room?> GetByCodeAsync(
        RoomCode code,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Rooms
            .AsNoTracking()
            .SingleOrDefaultAsync(room => room.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Rooms
            .AsNoTracking()
            .OrderBy(room => room.Code)
            .ToListAsync(cancellationToken);
    }
}
