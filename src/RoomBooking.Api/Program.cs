using System.Text.Json.Serialization;
using RoomBooking.Api.Authentication;
using RoomBooking.Api.Bookings;
using RoomBooking.Api.Chat;
using RoomBooking.Application;
using RoomBooking.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiAuthentication(builder.Environment);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" })).AllowAnonymous();
app.MapAuthenticationEndpoints();
app.MapBookingEndpoints();
app.MapChatEndpoints();

app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await context.Response.SendFileAsync(
        Path.Combine(app.Environment.WebRootPath, "index.html"),
        context.RequestAborted);
}).AllowAnonymous();

app.Run();

public partial class Program;
