using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RoomBooking.Api.Tests;

[TestClass]
public class AuthenticationEndpointTests : IDisposable
{
    private const string ChallengePassword = "TechnicalChallengePromtior";
    private WebApplicationFactory<Program>? factory;

    [TestInitialize]
    public void Initialize()
    {
        factory = new RoomBookingWebApplicationFactory();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    [DataRow("User1")]
    [DataRow("User2")]
    public async Task LoginAuthenticatesConfiguredUser(string userName)
    {
        using var client = CreateClient();

        var loginResponse = await LoginAsync(client, userName, ChallengePassword);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var currentUserResponse = await client.GetAsync("/api/auth/me");
        var currentUser = await currentUserResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();

        currentUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        currentUser.Should().Be(new CurrentUserResponse(userName));
    }

    [TestMethod]
    public async Task LoginRejectsInvalidPassword()
    {
        using var client = CreateClient();

        var response = await LoginAsync(client, "User1", "invalid-password");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Contains(HeaderNames.SetCookie).Should().BeFalse();
    }

    [TestMethod]
    public async Task LoginRejectsUnknownUser()
    {
        using var client = CreateClient();

        var response = await LoginAsync(client, "UnknownUser", ChallengePassword);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Contains(HeaderNames.SetCookie).Should().BeFalse();
    }

    [TestMethod]
    public async Task CurrentUserRejectsAnonymousRequestWithoutRedirecting()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [TestMethod]
    public async Task AuthenticationCookieUsesSecureBrowserSettingsOutsideDevelopment()
    {
        using var productionFactory = factory!.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production"));
        using var client = CreateClient(productionFactory);

        var response = await LoginAsync(client, "User1", ChallengePassword);
        var setCookieHeaders = response.Headers.GetValues(HeaderNames.SetCookie);
        var authenticationCookie = setCookieHeaders.Single(header =>
            header.StartsWith("RoomBooking.Auth=", StringComparison.Ordinal));
        var normalizedCookie = authenticationCookie.ToLowerInvariant();

        normalizedCookie.Should().Contain("httponly");
        normalizedCookie.Should().Contain("secure");
        normalizedCookie.Should().Contain("samesite=lax");
    }

    [TestMethod]
    public async Task LogoutRejectsRequestWithoutCsrfToken()
    {
        using var client = CreateClient();
        await LoginAsync(client, "User1", ChallengePassword);

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var currentUserResponse = await client.GetAsync("/api/auth/me");
        currentUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task LogoutWithCsrfTokenEndsAuthenticatedSession()
    {
        using var client = CreateClient();
        await LoginAsync(client, "User1", ChallengePassword);

        var csrfResponse = await client.GetAsync("/api/auth/csrf");
        var csrfToken = await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("X-CSRF-TOKEN", csrfToken!.Token);

        var logoutResponse = await client.SendAsync(logoutRequest);
        var currentUserResponse = await client.GetAsync("/api/auth/me");

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        currentUserResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient(WebApplicationFactory<Program>? applicationFactory = null)
    {
        return (applicationFactory ?? factory!).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
    }

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string userName,
        string password)
    {
        return client.PostAsJsonAsync("/api/auth/login", new { userName, password });
    }

    private sealed record CurrentUserResponse(string UserName);

    private sealed record CsrfTokenResponse(string Token);
}
