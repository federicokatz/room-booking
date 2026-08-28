using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Common;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Bookings.GetRoomSchedule;

public sealed record GetRoomScheduleQuery(
    string? RoomCode,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc);

public sealed record RoomScheduleResponse(
    string RoomCode,
    IReadOnlyList<RoomScheduleSlotResponse> Slots);

public sealed record RoomScheduleSlotResponse(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsOccupied);

public sealed class GetRoomScheduleUseCase(
    IRoomRepository roomRepository,
    IBookingRepository bookingRepository)
{
    public async Task<Result<RoomScheduleResponse>> ExecuteAsync(
        GetRoomScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        var roomCodeResult = RoomCode.Create(query.RoomCode);
        if (roomCodeResult.IsFailure)
        {
            return Result.Failure<RoomScheduleResponse>(roomCodeResult.Error!);
        }

        var rangeResult = RequestedTimeRange.Create(query.StartUtc, query.EndUtc);
        if (rangeResult.IsFailure)
        {
            return Result.Failure<RoomScheduleResponse>(rangeResult.Error!);
        }

        var room = await roomRepository.GetByCodeAsync(roomCodeResult.Value, cancellationToken);
        if (room is null)
        {
            return Result.Failure<RoomScheduleResponse>(BookingApplicationErrors.RoomNotFound);
        }

        var bookings = await bookingRepository.ListActiveForRoomAsync(
            room.Id,
            rangeResult.Value.StartUtc,
            rangeResult.Value.EndUtc,
            cancellationToken);

        var slots = new List<RoomScheduleSlotResponse>();
        for (var slotStart = rangeResult.Value.StartUtc;
             slotStart < rangeResult.Value.EndUtc;
             slotStart = slotStart.Add(BookingPeriod.SlotDuration))
        {
            var slotEnd = slotStart.Add(BookingPeriod.SlotDuration);
            var isOccupied = bookings.Any(booking =>
                booking.Period.StartUtc < slotEnd && slotStart < booking.Period.EndUtc);

            slots.Add(new RoomScheduleSlotResponse(slotStart, slotEnd, isOccupied));
        }

        return Result.Success(new RoomScheduleResponse(room.Code.Value, slots));
    }

    private sealed record RequestedTimeRange(DateTimeOffset StartUtc, DateTimeOffset EndUtc)
    {
        public static Result<RequestedTimeRange> Create(
            DateTimeOffset startUtc,
            DateTimeOffset endUtc)
        {
            if (startUtc.Offset != TimeSpan.Zero)
            {
                return Result.Failure<RequestedTimeRange>(
                    BookingApplicationErrors.RangeStartMustBeUtc);
            }

            if (endUtc.Offset != TimeSpan.Zero)
            {
                return Result.Failure<RequestedTimeRange>(
                    BookingApplicationErrors.RangeEndMustBeUtc);
            }

            if (startUtc.TimeOfDay.Ticks % BookingPeriod.SlotDuration.Ticks != 0)
            {
                return Result.Failure<RequestedTimeRange>(
                    BookingApplicationErrors.RangeStartMustAlignToSlot);
            }

            if (endUtc.TimeOfDay.Ticks % BookingPeriod.SlotDuration.Ticks != 0)
            {
                return Result.Failure<RequestedTimeRange>(
                    BookingApplicationErrors.RangeEndMustAlignToSlot);
            }

            return endUtc > startUtc
                ? Result.Success(new RequestedTimeRange(startUtc, endUtc))
                : Result.Failure<RequestedTimeRange>(
                    BookingApplicationErrors.RangeDurationMustBePositive);
        }
    }
}
