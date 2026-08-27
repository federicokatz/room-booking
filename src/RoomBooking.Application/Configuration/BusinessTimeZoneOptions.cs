namespace RoomBooking.Application.Configuration;

public sealed class BusinessTimeZoneOptions
{
    public const string SectionName = "BusinessTimeZone";

    public const string DefaultId = "America/Montevideo";

    public string Id { get; init; } = DefaultId;
}
