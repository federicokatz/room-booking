using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Configuration;
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

        return services;
    }
}
