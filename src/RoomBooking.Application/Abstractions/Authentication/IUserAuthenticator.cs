namespace RoomBooking.Application.Abstractions.Authentication;

public interface IUserAuthenticator
{
    AuthenticatedUser? Authenticate(
        string userName,
        string password);
}
