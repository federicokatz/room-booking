using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Domain.Tests.Rooms;

[TestClass]
public class RoomCodeTests
{
    [TestMethod]
    [DataRow("A", "A")]
    [DataRow("b", "B")]
    [DataRow(" C ", "C")]
    [DataRow("d", "D")]
    [DataRow("E", "E")]
    public void CreateAcceptsKnownCodes(string input, string expected)
    {
        var result = RoomCode.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("F")]
    [DataRow("Room A")]
    public void CreateRejectsUnknownCodes(string? input)
    {
        var result = RoomCode.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RoomErrors.InvalidCode);
    }

    [TestMethod]
    public void AllContainsExactlyFiveUniqueCodes()
    {
        RoomCode.All.Select(code => code.Value)
            .Should()
            .BeEquivalentTo(["A", "B", "C", "D", "E"]);
    }
}
