using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Domain.Tests.Bookings;

[TestClass]
public class BookingTests
{
    private static readonly DateTimeOffset DefaultCancellationTime = Utc(9);

    [TestMethod]
    public void CreateReturnsActiveBookingAndNormalizesText()
    {
        var id = Guid.NewGuid();
        var room = CreateRoom(RoomCode.A, 4);
        var period = CreatePeriod(10, 11);

        var result = Booking.Create(id, room, " User1 ", " Interview with John ", 3, period);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.RoomId.Should().Be(room.Id);
        result.Value.OwnerId.Should().Be("User1");
        result.Value.Title.Should().Be("Interview with John");
        result.Value.Attendees.Should().Be(3);
        result.Value.Status.Should().Be(BookingStatus.Active);
        result.Value.CancelledAtUtc.Should().BeNull();
    }

    [TestMethod]
    public void CreateRejectsEmptyIdentifier()
    {
        var result = Booking.Create(Guid.Empty, CreateRoom(), "User1", "Planning", 2, CreatePeriod());

        result.Error.Should().Be(BookingErrors.IdRequired);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void CreateRejectsMissingOwner(string? owner)
    {
        var result = Booking.Create(Guid.NewGuid(), CreateRoom(), owner, "Planning", 2, CreatePeriod());

        result.Error.Should().Be(BookingErrors.OwnerRequired);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void CreateRejectsMissingTitle(string? title)
    {
        var result = Booking.Create(Guid.NewGuid(), CreateRoom(), "User1", title, 2, CreatePeriod());

        result.Error.Should().Be(BookingErrors.TitleRequired);
    }

    [TestMethod]
    public void CreateRejectsTitleLongerThanMaximum()
    {
        var title = new string('A', Booking.MaxTitleLength + 1);

        var result = Booking.Create(Guid.NewGuid(), CreateRoom(), "User1", title, 2, CreatePeriod());

        result.Error.Should().Be(BookingErrors.TitleTooLong);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void CreateRejectsNonPositiveAttendees(int attendees)
    {
        var result = Booking.Create(Guid.NewGuid(), CreateRoom(), "User1", "Planning", attendees, CreatePeriod());

        result.Error.Should().Be(BookingErrors.AttendeesMustBePositive);
    }

    [TestMethod]
    public void CreateRejectsAttendeesAboveCapacity()
    {
        var result = Booking.Create(Guid.NewGuid(), CreateRoom(capacity: 4), "User1", "Planning", 5, CreatePeriod());

        result.Error.Should().Be(BookingErrors.CapacityExceeded);
    }

    [TestMethod]
    public void ConflictsWithReturnsTrueForActiveOverlappingBookingInSameRoom()
    {
        var room = CreateRoom();
        var first = CreateBooking(room, CreatePeriod(10, 12));
        var second = CreateBooking(room, CreatePeriod(11, 13));

        first.ConflictsWith(second).Should().BeTrue();
    }

    [TestMethod]
    public void ConflictsWithReturnsFalseForAdjacentBooking()
    {
        var room = CreateRoom();
        var first = CreateBooking(room, CreatePeriod(10, 11));
        var second = CreateBooking(room, CreatePeriod(11, 12));

        first.ConflictsWith(second).Should().BeFalse();
    }

    [TestMethod]
    public void ConflictsWithReturnsFalseForDifferentRoom()
    {
        var first = CreateBooking(CreateRoom(RoomCode.A), CreatePeriod(10, 12));
        var second = CreateBooking(CreateRoom(RoomCode.B), CreatePeriod(10, 12));

        first.ConflictsWith(second).Should().BeFalse();
    }

    [TestMethod]
    public void ConflictsWithReturnsFalseWhenOneBookingIsCancelled()
    {
        var room = CreateRoom();
        var first = CreateBooking(room, CreatePeriod(10, 12));
        var second = CreateBooking(room, CreatePeriod(10, 12));
        second.Cancel("User1", DefaultCancellationTime);

        first.ConflictsWith(second).Should().BeFalse();
    }

    [TestMethod]
    public void CancelTransitionsOwnedBookingToCancelled()
    {
        var booking = CreateBooking();

        var result = booking.Cancel("User1", DefaultCancellationTime);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancelledAtUtc.Should().Be(DefaultCancellationTime);
    }

    [TestMethod]
    public void CancelRejectsDifferentOwnerWithoutChangingState()
    {
        var booking = CreateBooking();

        var result = booking.Cancel("User2", DefaultCancellationTime);

        result.Error.Should().Be(BookingErrors.NotOwner);
        booking.Status.Should().Be(BookingStatus.Active);
        booking.CancelledAtUtc.Should().BeNull();
    }

    [TestMethod]
    public void CancelRejectsRepeatedCancellation()
    {
        var booking = CreateBooking();
        booking.Cancel("User1", DefaultCancellationTime);

        var result = booking.Cancel("User1", DefaultCancellationTime.AddMinutes(1));

        result.Error.Should().Be(BookingErrors.AlreadyCancelled);
    }

    [TestMethod]
    public void CancelRejectsNonUtcTimestamp()
    {
        var booking = CreateBooking();
        var localTime = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.FromHours(-3));

        var result = booking.Cancel("User1", localTime);

        result.Error.Should().Be(BookingErrors.CancellationTimeMustBeUtc);
        booking.Status.Should().Be(BookingStatus.Active);
    }

    private static Booking CreateBooking(Room? room = null, BookingPeriod? period = null)
    {
        return Booking.Create(
            Guid.NewGuid(),
            room ?? CreateRoom(),
            "User1",
            "Planning",
            2,
            period ?? CreatePeriod()).Value;
    }

    private static Room CreateRoom(RoomCode? code = null, int capacity = 4)
    {
        return Room.Create(Guid.NewGuid(), code ?? RoomCode.A, capacity).Value;
    }

    private static BookingPeriod CreatePeriod(int startHour = 10, int endHour = 11)
    {
        return BookingPeriod.Create(Utc(startHour), Utc(endHour)).Value;
    }

    private static DateTimeOffset Utc(int hour)
    {
        return new DateTimeOffset(2026, 8, 28, hour, 0, 0, TimeSpan.Zero);
    }
}
