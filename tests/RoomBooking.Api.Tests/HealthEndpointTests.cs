using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RoomBooking.Api.Tests;

[TestClass]
public class HealthEndpointTests : IDisposable
{
    private WebApplicationFactory<Program>? factory;

    [TestInitialize]
    public void Initialize()
    {
        factory = new WebApplicationFactory<Program>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task GetHealthReturnsOk()
    {
        using var client = factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
