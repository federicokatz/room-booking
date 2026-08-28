using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Bookings;

namespace RoomBooking.Application.Tests.Fakes;

internal sealed class FakeBookingRepository : IBookingRepository
{
    public List<Booking> Bookings { get; } = [];

    public bool ForceOverlap { get; set; }

    public bool ForceDatabaseConflict { get; set; }

    public int SaveChangesCount { get; private set; }

    public Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Bookings.SingleOrDefault(booking => booking.Id == id));
    }

    public Task<bool> HasActiveOverlapAsync(
        Guid roomId,
        BookingPeriod period,
        CancellationToken cancellationToken = default)
    {
        var hasOverlap = ForceOverlap || Bookings.Any(booking =>
            booking.Status == BookingStatus.Active
            && booking.RoomId == roomId
            && booking.Period.Overlaps(period));

        return Task.FromResult(hasOverlap);
    }

    public Task<IReadOnlySet<Guid>> ListOccupiedRoomIdsAsync(
        BookingPeriod period,
        CancellationToken cancellationToken = default)
    {
        IReadOnlySet<Guid> roomIds = Bookings
            .Where(booking => booking.Status == BookingStatus.Active
                && booking.Period.Overlaps(period))
            .Select(booking => booking.RoomId)
            .ToHashSet();

        return Task.FromResult(roomIds);
    }

    public Task<IReadOnlyList<Booking>> ListActiveForRoomAsync(
        Guid roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Booking> bookings = Bookings
            .Where(booking => booking.Status == BookingStatus.Active
                && booking.RoomId == roomId
                && booking.Period.StartUtc < endUtc
                && startUtc < booking.Period.EndUtc)
            .OrderBy(booking => booking.Period.StartUtc)
            .ToArray();

        return Task.FromResult(bookings);
    }

    public Task<IReadOnlyList<Booking>> ListActiveForOwnerAsync(
        string ownerId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Booking> bookings = Bookings
            .Where(booking => booking.Status == BookingStatus.Active
                && booking.OwnerId == ownerId
                && booking.Period.EndUtc > fromUtc)
            .OrderBy(booking => booking.Period.StartUtc)
            .ToArray();

        return Task.FromResult(bookings);
    }

    public Task<bool> TryAddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        if (ForceDatabaseConflict)
        {
            return Task.FromResult(false);
        }

        Bookings.Add(booking);
        return Task.FromResult(true);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCount++;
        return Task.CompletedTask;
    }
}
