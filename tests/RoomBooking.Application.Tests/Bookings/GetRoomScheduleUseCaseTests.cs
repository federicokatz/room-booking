using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Bookings.GetRoomSchedule;
using RoomBooking.Application.Tests.Fakes;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Bookings;

[TestClass]
public class GetRoomScheduleUseCaseTests
{
    [TestMethod]
    public async Task ExecuteReturnsOrderedFreeAndOccupiedSlots()
    {
        var room = TestData.CreateRoom(RoomCode.B, 6);
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(TestData.CreateBooking(
            room,
            "User1",
            TestData.Utc(10, 30),
            TestData.Utc(11, 30)));
        var useCase = new GetRoomScheduleUseCase(
            new FakeRoomRepository([room]),
            bookings);

        var result = await useCase.ExecuteAsync(new GetRoomScheduleQuery(
            "B",
            TestData.Utc(10),
            TestData.Utc(12)));

        result.Value.Slots.Select(slot => slot.IsOccupied)
            .Should().Equal(false, true, true, false);
    }

    [TestMethod]
    public async Task ExecuteAllowsScheduleRangeLongerThanBookingMaximum()
    {
        var room = TestData.CreateRoom(RoomCode.B, 6);
        var useCase = new GetRoomScheduleUseCase(
            new FakeRoomRepository([room]),
            new FakeBookingRepository());

        var result = await useCase.ExecuteAsync(new GetRoomScheduleQuery(
            "B",
            TestData.Utc(8),
            TestData.Utc(12)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Should().HaveCount(8);
    }
}
