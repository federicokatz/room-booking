using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Abstractions.Time;

namespace RoomBooking.Api.Tests;

[TestClass]
public class HealthEndpointTests : IDisposable
{
    private WebApplicationFactory<Program>? factory;

    [TestInitialize]
    public void Initialize()
    {
        factory = new RoomBookingWebApplicationFactory();
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

    [TestMethod]
    public void ApplicationUsesConfiguredBusinessTimeZone()
    {
        var businessTimeZone = factory!.Services.GetRequiredService<IBusinessTimeZone>();

        businessTimeZone.Value.Id.Should().Be("America/Montevideo");
    }
}
