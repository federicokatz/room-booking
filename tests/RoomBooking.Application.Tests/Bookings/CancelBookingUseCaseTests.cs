using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Bookings.CancelBooking;
using RoomBooking.Application.Tests.Fakes;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Bookings;

[TestClass]
public class CancelBookingUseCaseTests
{
    [TestMethod]
    public async Task ExecuteCancelsOwnedBookingAndPersistsChange()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var booking = TestData.CreateBooking(
            room,
            "User1",
            TestData.Utc(10),
            TestData.Utc(11));
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(booking);
        var useCase = CreateUseCase("User1", room, bookings);

        var result = await useCase.ExecuteAsync(booking.Id);

        result.Value.Status.Should().Be(BookingStatus.Cancelled);
        bookings.SaveChangesCount.Should().Be(1);
    }

    [TestMethod]
    public async Task ExecuteRejectsCancellationByAnotherUser()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var booking = TestData.CreateBooking(
            room,
            "User1",
            TestData.Utc(10),
            TestData.Utc(11));
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(booking);
        var useCase = CreateUseCase("User2", room, bookings);

        var result = await useCase.ExecuteAsync(booking.Id);

        result.Error.Should().Be(BookingErrors.NotOwner);
        bookings.SaveChangesCount.Should().Be(0);
    }

    [TestMethod]
    public async Task ExecuteReturnsNotFoundForUnknownBooking()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var useCase = CreateUseCase("User1", room, new FakeBookingRepository());

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        result.Error.Should().Be(BookingApplicationErrors.BookingNotFound);
    }

    private static CancelBookingUseCase CreateUseCase(
        string userName,
        Room room,
        FakeBookingRepository bookings)
    {
        return new CancelBookingUseCase(
            new FakeCurrentUser(userName),
            new FakeRoomRepository([room]),
            bookings,
            new StubTimeProvider(TestData.Utc(9)));
    }
}
