using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Bookings.ListMyBookings;
using RoomBooking.Application.Tests.Fakes;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Bookings;

[TestClass]
public class ListMyBookingsUseCaseTests
{
    [TestMethod]
    public async Task ExecuteReturnsOnlyCurrentUsersActiveUpcomingBookings()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(TestData.CreateBooking(
            room,
            "User1",
            TestData.Utc(10),
            TestData.Utc(11)));
        bookings.Bookings.Add(TestData.CreateBooking(
            room,
            "User2",
            TestData.Utc(11),
            TestData.Utc(12)));
        var useCase = new ListMyBookingsUseCase(
            new FakeCurrentUser("User1"),
            new FakeRoomRepository([room]),
            bookings,
            new StubTimeProvider(TestData.Utc(9)));

        var result = await useCase.ExecuteAsync();

        result.Value.Should().ContainSingle()
            .Which.Title.Should().Be("Planning");
    }
}
