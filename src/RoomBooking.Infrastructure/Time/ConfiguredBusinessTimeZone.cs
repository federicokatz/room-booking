using Microsoft.Extensions.Options;
using RoomBooking.Application.Abstractions.Time;
using RoomBooking.Application.Configuration;

namespace RoomBooking.Infrastructure.Time;

internal sealed class ConfiguredBusinessTimeZone : IBusinessTimeZone
{
    public ConfiguredBusinessTimeZone(IOptions<BusinessTimeZoneOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Value = TimeZoneInfo.FindSystemTimeZoneById(options.Value.Id);
    }

    public TimeZoneInfo Value { get; }
}
