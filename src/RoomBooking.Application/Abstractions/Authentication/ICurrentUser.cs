namespace RoomBooking.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string? UserName { get; }
}
