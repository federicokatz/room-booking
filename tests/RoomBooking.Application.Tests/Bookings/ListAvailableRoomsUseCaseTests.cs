using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Bookings.ListAvailableRooms;
using RoomBooking.Application.Tests.Fakes;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Bookings;

[TestClass]
public class ListAvailableRoomsUseCaseTests
{
    [TestMethod]
    public async Task ExecuteFiltersByCapacityAndOccupancy()
    {
        var roomA = TestData.CreateRoom(RoomCode.A, 4);
        var roomB = TestData.CreateRoom(RoomCode.B, 6);
        var roomC = TestData.CreateRoom(RoomCode.C, 8);
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(TestData.CreateBooking(
            roomB,
            "User1",
            TestData.Utc(10),
            TestData.Utc(11)));
        var useCase = new ListAvailableRoomsUseCase(
            new FakeRoomRepository([roomA, roomB, roomC]),
            bookings);

        var result = await useCase.ExecuteAsync(new ListAvailableRoomsQuery(
            TestData.Utc(10),
            TestData.Utc(11),
            5));

        result.Value.Should().ContainSingle()
            .Which.Code.Should().Be("C");
    }

    [TestMethod]
    public async Task ExecuteRejectsNonPositiveAttendees()
    {
        var useCase = new ListAvailableRoomsUseCase(
            new FakeRoomRepository([]),
            new FakeBookingRepository());

        var result = await useCase.ExecuteAsync(new ListAvailableRoomsQuery(
            TestData.Utc(10),
            TestData.Utc(11),
            0));

        result.Error.Should().Be(BookingErrors.AttendeesMustBePositive);
    }
}
