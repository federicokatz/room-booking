using RoomBooking.Application.Abstractions.Authentication;
using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Common;

namespace RoomBooking.Application.Bookings.ListMyBookings;

public sealed class ListMyBookingsUseCase(
    ICurrentUser currentUser,
    IRoomRepository roomRepository,
    IBookingRepository bookingRepository,
    TimeProvider timeProvider)
{
    public async Task<Result<IReadOnlyList<BookingResponse>>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserName))
        {
            return Result.Failure<IReadOnlyList<BookingResponse>>(
                BookingApplicationErrors.NotAuthenticated);
        }

        var bookings = await bookingRepository.ListActiveForOwnerAsync(
            currentUser.UserName,
            timeProvider.GetUtcNow(),
            cancellationToken);
        var rooms = await roomRepository.ListAsync(cancellationToken);
        var roomsById = rooms.ToDictionary(room => room.Id);

        var responses = bookings
            .Select(booking => BookingResponse.From(booking, roomsById[booking.RoomId]))
            .ToArray();

        return Result.Success<IReadOnlyList<BookingResponse>>(responses);
    }
}
