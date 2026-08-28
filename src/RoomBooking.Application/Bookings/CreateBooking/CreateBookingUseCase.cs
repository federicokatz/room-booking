using RoomBooking.Application.Abstractions.Authentication;
using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Common;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Bookings.CreateBooking;

public sealed record CreateBookingCommand(
    string? RoomCode,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string? Title,
    int Attendees);

public sealed class CreateBookingUseCase(
    ICurrentUser currentUser,
    IRoomRepository roomRepository,
    IBookingRepository bookingRepository)
{
    public async Task<Result<BookingResponse>> ExecuteAsync(
        CreateBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserName))
        {
            return Result.Failure<BookingResponse>(BookingApplicationErrors.NotAuthenticated);
        }

        var roomCodeResult = RoomCode.Create(command.RoomCode);
        if (roomCodeResult.IsFailure)
        {
            return Result.Failure<BookingResponse>(roomCodeResult.Error!);
        }

        var periodResult = BookingPeriod.Create(command.StartUtc, command.EndUtc);
        if (periodResult.IsFailure)
        {
            return Result.Failure<BookingResponse>(periodResult.Error!);
        }

        var room = await roomRepository.GetByCodeAsync(roomCodeResult.Value, cancellationToken);
        if (room is null)
        {
            return Result.Failure<BookingResponse>(BookingApplicationErrors.RoomNotFound);
        }

        var bookingResult = Booking.Create(
            Guid.NewGuid(),
            room,
            currentUser.UserName,
            command.Title,
            command.Attendees,
            periodResult.Value);

        if (bookingResult.IsFailure)
        {
            return Result.Failure<BookingResponse>(bookingResult.Error!);
        }

        if (await bookingRepository.HasActiveOverlapAsync(
                room.Id,
                periodResult.Value,
                cancellationToken))
        {
            return Result.Failure<BookingResponse>(BookingErrors.Overlap);
        }

        if (!await bookingRepository.TryAddAsync(bookingResult.Value, cancellationToken))
        {
            return Result.Failure<BookingResponse>(BookingErrors.Overlap);
        }

        return Result.Success(BookingResponse.From(bookingResult.Value, room));
    }
}
