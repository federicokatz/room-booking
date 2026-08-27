using RoomBooking.Domain.Common;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Domain.Bookings;

public sealed class Booking
{
    public const int MaxTitleLength = 200;

    private Booking(
        Guid id,
        Guid roomId,
        string ownerId,
        string title,
        int attendees,
        BookingPeriod period)
    {
        Id = id;
        RoomId = roomId;
        OwnerId = ownerId;
        Title = title;
        Attendees = attendees;
        Period = period;
        Status = BookingStatus.Active;
    }

    public Guid Id { get; }

    public Guid RoomId { get; }

    public string OwnerId { get; }

    public string Title { get; }

    public int Attendees { get; }

    public BookingPeriod Period { get; }

    public BookingStatus Status { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public static Result<Booking> Create(
        Guid id,
        Room room,
        string? ownerId,
        string? title,
        int attendees,
        BookingPeriod period)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(period);

        if (id == Guid.Empty)
        {
            return Result.Failure<Booking>(BookingErrors.IdRequired);
        }

        var normalizedOwnerId = ownerId?.Trim();
        if (string.IsNullOrEmpty(normalizedOwnerId))
        {
            return Result.Failure<Booking>(BookingErrors.OwnerRequired);
        }

        var normalizedTitle = title?.Trim();
        if (string.IsNullOrEmpty(normalizedTitle))
        {
            return Result.Failure<Booking>(BookingErrors.TitleRequired);
        }

        if (normalizedTitle.Length > MaxTitleLength)
        {
            return Result.Failure<Booking>(BookingErrors.TitleTooLong);
        }

        if (attendees <= 0)
        {
            return Result.Failure<Booking>(BookingErrors.AttendeesMustBePositive);
        }

        return room.CanHost(attendees)
            ? Result.Success(new Booking(id, room.Id, normalizedOwnerId, normalizedTitle, attendees, period))
            : Result.Failure<Booking>(BookingErrors.CapacityExceeded);
    }

    public bool ConflictsWith(Booking other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Status == BookingStatus.Active
            && other.Status == BookingStatus.Active
            && RoomId == other.RoomId
            && Period.Overlaps(other.Period);
    }

    public Result<Booking> Cancel(string? requestedBy, DateTimeOffset cancelledAtUtc)
    {
        if (!string.Equals(OwnerId, requestedBy?.Trim(), StringComparison.Ordinal))
        {
            return Result.Failure<Booking>(BookingErrors.NotOwner);
        }

        if (Status == BookingStatus.Cancelled)
        {
            return Result.Failure<Booking>(BookingErrors.AlreadyCancelled);
        }

        if (cancelledAtUtc.Offset != TimeSpan.Zero)
        {
            return Result.Failure<Booking>(BookingErrors.CancellationTimeMustBeUtc);
        }

        Status = BookingStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;

        return Result.Success(this);
    }
}
