using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RoomBooking.Api.Security;
using RoomBooking.Application.Abstractions.Authentication;

namespace RoomBooking.Api.Authentication;

internal static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapGet("/me", GetCurrentUser).RequireAuthorization();
        group.MapGet("/csrf", GetCsrfToken).RequireAuthorization();
        group.MapPost(
                "/logout",
                (Func<HttpContext, Task<IResult>>)LogoutAsync)
            .RequireAuthorization()
            .RequireValidAntiforgeryToken();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IUserAuthenticator authenticator,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return Results.Unauthorized();
        }

        var authenticatedUser = authenticator.Authenticate(
            request.UserName,
            request.Password);

        if (authenticatedUser is null)
        {
            return Results.Unauthorized();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, authenticatedUser.UserName),
            new Claim(ClaimTypes.Name, authenticatedUser.UserName)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Results.Ok(new CurrentUserResponse(authenticatedUser.UserName));
    }

    private static IResult GetCurrentUser(ICurrentUser currentUser)
    {
        return currentUser.UserName is { } userName
            ? Results.Ok(new CurrentUserResponse(userName))
            : Results.Unauthorized();
    }

    private static IResult GetCsrfToken(HttpContext httpContext, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);

        return Results.Ok(new CsrfTokenResponse(tokens.RequestToken!));
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.NoContent();
    }

    private sealed record LoginRequest(string? UserName, string? Password);

    private sealed record CurrentUserResponse(string UserName);

    private sealed record CsrfTokenResponse(string Token);
}
