using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Application.Rooms;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Common;

namespace RoomBooking.Application.Bookings.ListAvailableRooms;

public sealed record ListAvailableRoomsQuery(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int Attendees);

public sealed class ListAvailableRoomsUseCase(
    IRoomRepository roomRepository,
    IBookingRepository bookingRepository)
{
    public async Task<Result<IReadOnlyList<RoomResponse>>> ExecuteAsync(
        ListAvailableRoomsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Attendees <= 0)
        {
            return Result.Failure<IReadOnlyList<RoomResponse>>(
                BookingErrors.AttendeesMustBePositive);
        }

        var periodResult = BookingPeriod.Create(query.StartUtc, query.EndUtc);
        if (periodResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RoomResponse>>(periodResult.Error!);
        }

        var rooms = await roomRepository.ListAsync(cancellationToken);
        var occupiedRoomIds = await bookingRepository.ListOccupiedRoomIdsAsync(
            periodResult.Value,
            cancellationToken);

        var availableRooms = rooms
            .Where(room => room.CanHost(query.Attendees) && !occupiedRoomIds.Contains(room.Id))
            .OrderBy(room => room.Code.Value, StringComparer.Ordinal)
            .Select(RoomResponse.From)
            .ToArray();

        return Result.Success<IReadOnlyList<RoomResponse>>(availableRooms);
    }
}
