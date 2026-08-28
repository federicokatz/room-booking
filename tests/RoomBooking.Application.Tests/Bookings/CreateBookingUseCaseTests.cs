using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Bookings.CreateBooking;
using RoomBooking.Application.Tests.Fakes;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Bookings;

[TestClass]
public class CreateBookingUseCaseTests
{
    [TestMethod]
    public async Task ExecuteCreatesBookingForCurrentUser()
    {
        var room = TestData.CreateRoom(RoomCode.B, 6);
        var bookings = new FakeBookingRepository();
        var useCase = CreateUseCase("User1", room, bookings);

        var result = await useCase.ExecuteAsync(new CreateBookingCommand(
            "B",
            TestData.Utc(10),
            TestData.Utc(11),
            "Interview",
            4));

        result.IsSuccess.Should().BeTrue();
        result.Value.RoomCode.Should().Be("B");
        bookings.Bookings.Should().ContainSingle()
            .Which.OwnerId.Should().Be("User1");
    }

    [TestMethod]
    public async Task ExecuteReturnsDomainCapacityError()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var bookings = new FakeBookingRepository();
        var useCase = CreateUseCase("User1", room, bookings);

        var result = await useCase.ExecuteAsync(new CreateBookingCommand(
            "A",
            TestData.Utc(10),
            TestData.Utc(11),
            "Large meeting",
            5));

        result.Error.Should().Be(BookingErrors.CapacityExceeded);
        bookings.Bookings.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ExecuteReturnsFriendlyOverlapBeforeInsert()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var bookings = new FakeBookingRepository { ForceOverlap = true };
        var useCase = CreateUseCase("User1", room, bookings);

        var result = await useCase.ExecuteAsync(new CreateBookingCommand(
            "A",
            TestData.Utc(10),
            TestData.Utc(11),
            "Planning",
            2));

        result.Error.Should().Be(BookingErrors.Overlap);
        bookings.Bookings.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ExecuteMapsDatabaseRaceConflictToOverlap()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var bookings = new FakeBookingRepository { ForceDatabaseConflict = true };
        var useCase = CreateUseCase("User1", room, bookings);

        var result = await useCase.ExecuteAsync(new CreateBookingCommand(
            "A",
            TestData.Utc(10),
            TestData.Utc(11),
            "Planning",
            2));

        result.Error.Should().Be(BookingErrors.Overlap);
    }

    [TestMethod]
    public async Task ExecuteRejectsBookingThatStartsInThePast()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var bookings = new FakeBookingRepository();
        var useCase = CreateUseCase(
            "User1",
            room,
            bookings,
            new StubTimeProvider(TestData.Utc(11)));

        var result = await useCase.ExecuteAsync(new CreateBookingCommand(
            "A",
            TestData.Utc(10),
            TestData.Utc(11),
            "Planning",
            2));

        result.Error.Should().Be(BookingApplicationErrors.StartMustBeInFuture);
        bookings.Bookings.Should().BeEmpty();
    }

    private static CreateBookingUseCase CreateUseCase(
        string? userName,
        Room room,
        FakeBookingRepository bookings,
        TimeProvider? timeProvider = null)
    {
        return new CreateBookingUseCase(
            new FakeCurrentUser(userName),
            new FakeRoomRepository([room]),
            bookings,
            timeProvider ?? new StubTimeProvider(TestData.Utc(9)));
    }
}
