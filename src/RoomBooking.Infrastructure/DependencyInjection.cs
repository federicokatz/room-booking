using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using RoomBooking.Application.Abstractions.Authentication;
using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Configuration;
using RoomBooking.Infrastructure.Authentication;
using RoomBooking.Infrastructure.Time;

namespace RoomBooking.Infrastructure;

public static class DependencyInjection
{
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
