using RoomBooking.Api.Authentication;
using RoomBooking.Application;
using RoomBooking.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiAuthentication(builder.Environment);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" })).AllowAnonymous();
app.MapAuthenticationEndpoints();

app.Run();

public partial class Program;
