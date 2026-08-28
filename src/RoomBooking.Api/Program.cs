using System.Text.Json.Serialization;
using RoomBooking.Api.Authentication;
using RoomBooking.Api.Bookings;
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
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" })).AllowAnonymous();
app.MapAuthenticationEndpoints();
app.MapBookingEndpoints();

app.Run();

public partial class Program;
