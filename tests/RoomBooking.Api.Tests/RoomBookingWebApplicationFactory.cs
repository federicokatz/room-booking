using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace RoomBooking.Api.Tests;

internal sealed class RoomBookingWebApplicationFactory(
    string connectionString =
        "Host=localhost;Port=1;Database=room_booking_tests;Username=test;Password=test")
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:RoomBooking",
            connectionString);
        builder.ConfigureServices(services =>
            services.AddDataProtection().UseEphemeralDataProtectionProvider());
    }
}
