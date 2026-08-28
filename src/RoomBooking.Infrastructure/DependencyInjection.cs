using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoomBooking.Application.Abstractions.Authentication;
using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Agent;
using RoomBooking.Application.Configuration;
using RoomBooking.Infrastructure.Ai;
using RoomBooking.Infrastructure.Authentication;
using RoomBooking.Infrastructure.Persistence;
using RoomBooking.Infrastructure.Persistence.Repositories;
using RoomBooking.Infrastructure.Time;

namespace RoomBooking.Infrastructure;

public static class DependencyInjection
{
    private const string DatabaseConnectionName = "RoomBooking";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<BusinessTimeZoneOptions>()
            .Bind(configuration.GetSection(BusinessTimeZoneOptions.SectionName))
            .Validate(
                options => TimeZoneInfo.TryFindSystemTimeZoneById(options.Id, out _),
                "BusinessTimeZone:Id must be a valid time zone identifier.")
            .ValidateOnStart();

        services.AddSingleton<IBusinessTimeZone, ConfiguredBusinessTimeZone>();

        services
            .AddOptions<AuthenticationOptions>()
            .Bind(configuration.GetSection(AuthenticationOptions.SectionName))
            .Validate(
                HasRequiredUsers,
                "Authentication must configure exactly User1 and User2 with password hashes.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher<ConfiguredUser>, PasswordHasher<ConfiguredUser>>();
        services.AddSingleton<IUserAuthenticator, ConfiguredUserAuthenticator>();

        services
            .AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName));
        services.AddHttpClient<IChatModel, GroqChatModel>(client =>
            client.Timeout = TimeSpan.FromSeconds(30));

        var connectionString = configuration.GetConnectionString(DatabaseConnectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{DatabaseConnectionName} must be configured.");
        }

        services.AddDbContext<RoomBookingDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }

    private static bool HasRequiredUsers(AuthenticationOptions options)
    {
        if (options.Users.Count != 2 || options.Users.Any(user =>
                string.IsNullOrWhiteSpace(user.UserName) ||
                string.IsNullOrWhiteSpace(user.PasswordHash)))
        {
            return false;
        }

        var configuredNames = options.Users
            .Select(user => user.UserName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return configuredNames.SetEquals(["User1", "User2"]);
    }
}
