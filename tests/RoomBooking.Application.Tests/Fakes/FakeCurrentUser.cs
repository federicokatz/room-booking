using RoomBooking.Application.Abstractions.Authentication;

namespace RoomBooking.Application.Tests.Fakes;

internal sealed class FakeCurrentUser(string? userName) : ICurrentUser
{
    public bool IsAuthenticated => UserName is not null;

    public string? UserName { get; } = userName;
}
