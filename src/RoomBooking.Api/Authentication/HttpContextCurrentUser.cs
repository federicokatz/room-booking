using System.Security.Claims;
using RoomBooking.Application.Abstractions.Authentication;

namespace RoomBooking.Api.Authentication;

internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated is true;

    public string? UserName => IsAuthenticated
        ? httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        : null;
}
