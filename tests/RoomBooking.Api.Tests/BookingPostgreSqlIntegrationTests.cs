using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Abstractions.Persistence;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;
using RoomBooking.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RoomBooking.Api.Tests;

[TestClass]
public class BookingPostgreSqlIntegrationTests : IDisposable
{
    private const string ChallengePassword = "TechnicalChallengePromtior";
    private static readonly JsonSerializerOptions WebJsonOptions =
        new(JsonSerializerDefaults.Web);
    private static PostgreSqlContainer? postgreSqlContainer;
    private static string? dockerUnavailableReason;
    private RoomBookingWebApplicationFactory? factory;

    [ClassInitialize]
    public static async Task InitializePostgreSql(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        PostgreSqlContainer? container = null;

        try
        {
            container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("room_booking_tests")
                .Build();
            await container.StartAsync();
            postgreSqlContainer = container;
        }
        catch (DockerUnavailableException exception) when (!IsContinuousIntegration())
        {
            dockerUnavailableReason = exception.Message;

            if (container is not null)
            {
                await container.DisposeAsync();
            }
        }
    }

    [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
    public static async Task CleanupPostgreSql()
    {
        if (postgreSqlContainer is not null)
        {
            await postgreSqlContainer.DisposeAsync();
        }
    }

    [TestInitialize]
    public async Task InitializeDatabase()
    {
        if (postgreSqlContainer is null)
        {
            Assert.Inconclusive($"Docker is required for PostgreSQL integration tests: {dockerUnavailableReason}");
            return;
        }

        factory = new RoomBookingWebApplicationFactory(postgreSqlContainer.GetConnectionString());

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RoomBookingDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    [TestCleanup]
    public void CleanupDatabase()
    {
        Dispose();
    }

    public void Dispose()
    {
        factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task RoomsEndpointReturnsSeededRoomsAndCapacities()
    {
        using var client = CreateClient();
        await AuthenticateAsync(client, "User1");

        var rooms = await client.GetFromJsonAsync<RoomApiResponse[]>("/api/rooms");

        rooms.Should().NotBeNull();
        rooms!.Select(room => (room.Code, room.Capacity)).Should().Equal(
            ("A", 4),
            ("B", 6),
            ("C", 8),
            ("D", 10),
            ("E", 12));
    }

    [TestMethod]
    public async Task CreateBookingPersistsAndListsCurrentUsersBooking()
    {
        using var client = CreateClient();
        var csrfToken = await AuthenticateAsync(client, "User1");

        using var createResponse = await CreateBookingAsync(
            client,
            csrfToken,
            "B",
            Utc(10),
            Utc(11),
            "Interview",
            4);
        var bookings = await client.GetFromJsonAsync<BookingApiResponse[]>("/api/bookings/mine");

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        bookings.Should().ContainSingle()
            .Which.Title.Should().Be("Interview");
    }

    [TestMethod]
    public async Task AdjacentBookingsInSameRoomBothSucceed()
    {
        using var client = CreateClient();
        var csrfToken = await AuthenticateAsync(client, "User1");

        using var firstResponse = await CreateBookingAsync(
            client,
            csrfToken,
            "A",
            Utc(10),
            Utc(11),
            "First",
            2);
        using var secondResponse = await CreateBookingAsync(
            client,
            csrfToken,
            "A",
            Utc(11),
            Utc(12),
            "Second",
            2);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task CreateBookingRejectsCapacityExceededWithStableCode()
    {
        using var client = CreateClient();
        var csrfToken = await AuthenticateAsync(client, "User1");

        using var response = await CreateBookingAsync(
            client,
            csrfToken,
            "A",
            Utc(10),
            Utc(11),
            "Too large",
            5);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().Should().Be("booking.capacity_exceeded");
    }

    [TestMethod]
    public async Task RoomScheduleShowsOccupancyWithoutExposingOwner()
    {
        using var client = CreateClient();
        var csrfToken = await AuthenticateAsync(client, "User1");
        using var createResponse = await CreateBookingAsync(
            client,
            csrfToken,
            "B",
            Utc(10, 30),
            Utc(11, 30),
            "Private meeting",
            2);
        createResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync(
            $"/api/rooms/B/schedule?startUtc={Encode(Utc(10))}&endUtc={Encode(Utc(12))}");
        var body = await response.Content.ReadAsStringAsync();
        var schedule = JsonSerializer.Deserialize<RoomScheduleApiResponse>(
            body,
            WebJsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotContain("User1");
        schedule!.Slots.Select(slot => slot.IsOccupied)
            .Should().Equal(false, true, true, false);
    }

    [TestMethod]
    public async Task OnlyOwnerCanCancelAndCancelledSlotCanBeRebooked()
    {
        using var user1Client = CreateClient();
        var user1CsrfToken = await AuthenticateAsync(user1Client, "User1");
        using var createResponse = await CreateBookingAsync(
            user1Client,
            user1CsrfToken,
            "C",
            Utc(10),
            Utc(11),
            "Planning",
            2);
        var booking = await createResponse.Content.ReadFromJsonAsync<BookingApiResponse>();

        using var user2Client = CreateClient();
        var user2CsrfToken = await AuthenticateAsync(user2Client, "User2");
        using var forbiddenResponse = await CancelBookingAsync(
            user2Client,
            user2CsrfToken,
            booking!.Id);
        using var cancelResponse = await CancelBookingAsync(
            user1Client,
            user1CsrfToken,
            booking.Id);
        using var replacementResponse = await CreateBookingAsync(
            user1Client,
            user1CsrfToken,
            "C",
            Utc(10),
            Utc(11),
            "Replacement",
            2);

        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        replacementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task CreateBookingRejectsAnonymousRequest()
    {
        using var client = CreateClient();
        using var request = CreateBookingRequest(
            "A",
            Utc(10),
            Utc(11),
            "Anonymous booking",
            2);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateBookingRejectsRequestWithoutCsrfToken()
    {
        using var client = CreateClient();
        await AuthenticateAsync(client, "User1");
        using var request = CreateBookingRequest(
            "A",
            Utc(10),
            Utc(11),
            "Missing CSRF",
            2);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CancelBookingRejectsRequestWithoutCsrfToken()
    {
        using var client = CreateClient();
        var csrfToken = await AuthenticateAsync(client, "User1");
        using var createResponse = await CreateBookingAsync(
            client,
            csrfToken,
            "A",
            Utc(10),
            Utc(11),
            "Protected cancellation",
            2);
        var booking = await createResponse.Content.ReadFromJsonAsync<BookingApiResponse>();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/bookings/{booking!.Id}/cancel");
        using var response = await client.SendAsync(request);
        var bookings = await client.GetFromJsonAsync<BookingApiResponse[]>("/api/bookings/mine");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        bookings.Should().ContainSingle(item => item.Id == booking.Id);
    }

    [TestMethod]
    public async Task RoomScheduleRejectsInvalidRoomCode()
    {
        using var client = CreateClient();
        await AuthenticateAsync(client, "User1");

        using var response = await client.GetAsync(
            $"/api/rooms/Z/schedule?startUtc={Encode(Utc(10))}&endUtc={Encode(Utc(11))}");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().Should().Be("room.invalid_code");
    }

    [TestMethod]
    public async Task CreateBookingReturnsConflictForOverlappingReservation()
    {
        using var client = CreateClient();
        var csrfToken = await AuthenticateAsync(client, "User1");
        using var firstResponse = await CreateBookingAsync(
            client,
            csrfToken,
            "A",
            Utc(10),
            Utc(11),
            "First booking",
            2);
        using var overlappingResponse = await CreateBookingAsync(
            client,
            csrfToken,
            "A",
            Utc(10, 30),
            Utc(11, 30),
            "Overlapping booking",
            2);
        var problem = await overlappingResponse.Content.ReadFromJsonAsync<JsonElement>();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        overlappingResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().Should().Be("booking.overlap");
    }

    [TestMethod]
    public async Task CancelBookingMapsNotFoundAndAlreadyCancelledErrors()
    {
        using var client = CreateClient();
        var csrfToken = await AuthenticateAsync(client, "User1");
        using var unknownResponse = await CancelBookingAsync(client, csrfToken, Guid.NewGuid());
        var unknownProblem = await unknownResponse.Content.ReadFromJsonAsync<JsonElement>();
        using var createResponse = await CreateBookingAsync(
            client,
            csrfToken,
            "A",
            Utc(10),
            Utc(11),
            "Cancellation lifecycle",
            2);
        var booking = await createResponse.Content.ReadFromJsonAsync<BookingApiResponse>();
        using var firstCancelResponse = await CancelBookingAsync(client, csrfToken, booking!.Id);
        using var secondCancelResponse = await CancelBookingAsync(client, csrfToken, booking.Id);
        var secondCancelProblem = await secondCancelResponse.Content.ReadFromJsonAsync<JsonElement>();

        unknownResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknownProblem.GetProperty("code").GetString().Should().Be("booking.not_found");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        firstCancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondCancelResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        secondCancelProblem.GetProperty("code").GetString().Should().Be("booking.already_cancelled");
    }

    [TestMethod]
    public async Task ExclusionConstraintAllowsExactlyOneCompetingInsert()
    {
        using var firstScope = factory!.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var firstRooms = firstScope.ServiceProvider.GetRequiredService<IRoomRepository>();
        var secondRooms = secondScope.ServiceProvider.GetRequiredService<IRoomRepository>();
        var firstBookings = firstScope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var secondBookings = secondScope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var firstRoom = await firstRooms.GetByCodeAsync(RoomCode.A);
        var secondRoom = await secondRooms.GetByCodeAsync(RoomCode.A);
        var period = BookingPeriod.Create(Utc(10), Utc(11)).Value;
        var firstBooking = Booking.Create(
            Guid.NewGuid(),
            firstRoom!,
            "User1",
            "Competing A",
            2,
            period).Value;
        var secondBooking = Booking.Create(
            Guid.NewGuid(),
            secondRoom!,
            "User2",
            "Competing B",
            2,
            period).Value;

        var results = await Task.WhenAll(
            firstBookings.TryAddAsync(firstBooking),
            secondBookings.TryAddAsync(secondBooking));

        results.Count(success => success).Should().Be(1);

        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<RoomBookingDbContext>();
        (await dbContext.Bookings.CountAsync()).Should().Be(1);
    }

    private HttpClient CreateClient()
    {
        return factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
    }

    private static async Task<string> AuthenticateAsync(HttpClient client, string userName)
    {
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { userName, password = ChallengePassword });
        loginResponse.EnsureSuccessStatusCode();

        var csrfToken = await client.GetFromJsonAsync<CsrfTokenResponse>("/api/auth/csrf");
        return csrfToken!.Token;
    }

    private static async Task<HttpResponseMessage> CreateBookingAsync(
        HttpClient client,
        string csrfToken,
        string roomCode,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string title,
        int attendees)
    {
        using var request = CreateBookingRequest(
            roomCode,
            startUtc,
            endUtc,
            title,
            attendees);
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CreateBookingRequest(
        string roomCode,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string title,
        int attendees)
    {
        return new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = JsonContent.Create(new { roomCode, startUtc, endUtc, title, attendees })
        };
    }

    private static async Task<HttpResponseMessage> CancelBookingAsync(
        HttpClient client,
        string csrfToken,
        Guid bookingId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/bookings/{bookingId}/cancel");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        return await client.SendAsync(request);
    }

    private static bool IsContinuousIntegration()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string Encode(DateTimeOffset value)
    {
        return Uri.EscapeDataString(value.ToString("O"));
    }

    private static DateTimeOffset Utc(int hour, int minute = 0)
    {
        return new DateTimeOffset(2026, 9, 1, hour, minute, 0, TimeSpan.Zero);
    }

    private sealed record CsrfTokenResponse(string Token);

    private sealed record RoomApiResponse(Guid Id, string Code, int Capacity);

    private sealed record BookingApiResponse(Guid Id, string Title, string Status);

    private sealed record RoomScheduleApiResponse(
        string RoomCode,
        IReadOnlyList<RoomScheduleSlotApiResponse> Slots);

    private sealed record RoomScheduleSlotApiResponse(
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        bool IsOccupied);
}
