using RoomBooking.Application.Abstractions.Authentication;
using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Common;

namespace RoomBooking.Application.Bookings.CancelBooking;

public sealed class CancelBookingUseCase(
    ICurrentUser currentUser,
    IRoomRepository roomRepository,
    IBookingRepository bookingRepository,
    TimeProvider timeProvider)
{
    public async Task<Result<BookingResponse>> ExecuteAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserName))
        {
            return Result.Failure<BookingResponse>(BookingApplicationErrors.NotAuthenticated);
        }

        if (bookingId == Guid.Empty)
        {
            return Result.Failure<BookingResponse>(BookingErrors.IdRequired);
        }

        var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<BookingResponse>(BookingApplicationErrors.BookingNotFound);
        }

        var room = await roomRepository.GetByIdAsync(booking.RoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<BookingResponse>(BookingApplicationErrors.RoomNotFound);
        }

        var cancellationResult = booking.Cancel(
            currentUser.UserName,
            timeProvider.GetUtcNow());
        if (cancellationResult.IsFailure)
        {
            return Result.Failure<BookingResponse>(cancellationResult.Error!);
        }

        await bookingRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(BookingResponse.From(booking, room));
    }
}
