using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Rooms;
using RoomBooking.Application.Tests.Fakes;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Rooms;

[TestClass]
public class ListRoomsUseCaseTests
{
    [TestMethod]
    public async Task ExecuteReturnsRoomsOrderedByCode()
    {
        var useCase = new ListRoomsUseCase(new FakeRoomRepository(
        [
            TestData.CreateRoom(RoomCode.C, 8),
            TestData.CreateRoom(RoomCode.A, 4),
            TestData.CreateRoom(RoomCode.B, 6)
        ]));

        var result = await useCase.ExecuteAsync();

        result.Select(room => room.Code).Should().Equal("A", "B", "C");
    }
}
