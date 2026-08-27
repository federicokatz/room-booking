using RoomBooking.Application;
using RoomBooking.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();

public partial class Program;
