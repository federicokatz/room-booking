using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Configuration;

namespace RoomBooking.Application.Tests.Configuration;

[TestClass]
public class BusinessTimeZoneOptionsTests
{
    [TestMethod]
    public void DefaultIdUsesDocumentedMontevideoTimeZone()
    {
        var options = new BusinessTimeZoneOptions();

        options.Id.Should().Be("America/Montevideo");
    }

    [TestMethod]
    public void IdCanBeConfiguredWithoutChangingBusinessCode()
    {
        var options = new BusinessTimeZoneOptions { Id = "UTC" };

        options.Id.Should().Be("UTC");
    }
}
