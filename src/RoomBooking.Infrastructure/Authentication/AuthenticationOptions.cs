namespace RoomBooking.Infrastructure.Authentication;

internal sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public List<ConfiguredUser> Users { get; init; } = [];
}
