namespace RoomBooking.Infrastructure.Authentication;

internal sealed class ConfiguredUser
{
    public string UserName { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;
}
