using Microsoft.EntityFrameworkCore;
using Npgsql;
using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Bookings;

namespace RoomBooking.Infrastructure.Persistence.Repositories;

internal sealed class BookingRepository(RoomBookingDbContext dbContext) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings.SingleOrDefaultAsync(
            booking => booking.Id == id,
            cancellationToken);
    }

    public Task<bool> HasActiveOverlapAsync(
        Guid roomId,
        BookingPeriod period,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(
                booking => booking.RoomId == roomId
                    && booking.Status == BookingStatus.Active
                    && booking.Period.StartUtc < period.EndUtc
                    && period.StartUtc < booking.Period.EndUtc,
                cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> ListOccupiedRoomIdsAsync(
        BookingPeriod period,
        CancellationToken cancellationToken = default)
    {
        var roomIds = await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.Status == BookingStatus.Active
                && booking.Period.StartUtc < period.EndUtc
                && period.StartUtc < booking.Period.EndUtc)
            .Select(booking => booking.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return roomIds.ToHashSet();
    }

    public async Task<IReadOnlyList<Booking>> ListActiveForRoomAsync(
        Guid roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.RoomId == roomId
                && booking.Status == BookingStatus.Active
                && booking.Period.StartUtc < endUtc
                && startUtc < booking.Period.EndUtc)
            .OrderBy(booking => booking.Period.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> ListActiveForOwnerAsync(
        string ownerId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.OwnerId == ownerId
                && booking.Status == BookingStatus.Active
                && booking.Period.EndUtc > fromUtc)
            .OrderBy(booking => booking.Period.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryAddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        dbContext.Bookings.Add(booking);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation
            })
        {
            dbContext.Entry(booking).State = EntityState.Detached;
            return false;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
