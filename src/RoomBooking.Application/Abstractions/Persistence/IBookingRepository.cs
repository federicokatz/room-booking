using RoomBooking.Domain.Bookings;

namespace RoomBooking.Application.Abstractions.Persistence;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasActiveOverlapAsync(
        Guid roomId,
        BookingPeriod period,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> ListOccupiedRoomIdsAsync(
        BookingPeriod period,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListActiveForRoomAsync(
        Guid roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListActiveForOwnerAsync(
        string ownerId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
