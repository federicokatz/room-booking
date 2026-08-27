using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Domain.Tests.Rooms;

[TestClass]
public class RoomTests
{
    [TestMethod]
    public void CreateReturnsRoomForValidValues()
    {
        var id = Guid.NewGuid();

        var result = Room.Create(id, RoomCode.A, 4);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.Code.Should().Be(RoomCode.A);
        result.Value.Capacity.Should().Be(4);
    }

    [TestMethod]
    public void CreateRejectsEmptyIdentifier()
    {
        var result = Room.Create(Guid.Empty, RoomCode.A, 4);

        result.Error.Should().Be(RoomErrors.IdRequired);
    }

    [TestMethod]
    public void CreateRejectsMissingCode()
    {
        var result = Room.Create(Guid.NewGuid(), null, 4);

        result.Error.Should().Be(RoomErrors.InvalidCode);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void CreateRejectsNonPositiveCapacity(int capacity)
    {
        var result = Room.Create(Guid.NewGuid(), RoomCode.A, capacity);

        result.Error.Should().Be(RoomErrors.CapacityMustBePositive);
    }

    [TestMethod]
    [DataRow(1, true)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(0, false)]
    public void CanHostRespectsCapacityBoundaries(int attendees, bool expected)
    {
        var room = Room.Create(Guid.NewGuid(), RoomCode.A, 4).Value;

        room.CanHost(attendees).Should().Be(expected);
    }
}
