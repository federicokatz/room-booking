using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RoomBooking.Application.Abstractions.Authentication;

namespace RoomBooking.Infrastructure.Authentication;

internal sealed class ConfiguredUserAuthenticator(
    IOptions<AuthenticationOptions> options,
    IPasswordHasher<ConfiguredUser> passwordHasher) : IUserAuthenticator
{
    public AuthenticatedUser? Authenticate(
        string userName,
        string password)
    {
        var configuredUser = options.Value.Users.SingleOrDefault(user =>
            string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase));

        if (configuredUser is null)
        {
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            configuredUser,
            configuredUser.PasswordHash,
            password);

        var authenticatedUser = verificationResult is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded
                ? new AuthenticatedUser(configuredUser.UserName)
                : null;

        return authenticatedUser;
    }
}
