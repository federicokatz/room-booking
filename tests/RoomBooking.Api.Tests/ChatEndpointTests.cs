using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Agent;

namespace RoomBooking.Api.Tests;

[TestClass]
public class ChatEndpointTests : IDisposable
{
    private const string ChallengePassword = "TechnicalChallengePromtior";
    private RoomBookingWebApplicationFactory? factory;

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
    public async Task CreateSessionRejectsAnonymousRequest()
    {
        var model = new ApiFakeChatModel();
        using var chatFactory = CreateFactory(model);
        using var client = CreateClient(chatFactory);

        using var response = await client.PostAsync("/api/chat/sessions", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateSessionRejectsMissingCsrfToken()
    {
        var model = new ApiFakeChatModel();
        using var chatFactory = CreateFactory(model);
        using var client = CreateClient(chatFactory);
        await LoginAsync(client, "User1");

        using var response = await client.PostAsync("/api/chat/sessions", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task AuthenticatedUserCanCreateUseAndDeleteChatSession()
    {
        var model = new ApiFakeChatModel(new ChatModelResponse(
            "Puedo ayudarte con las reservas.",
            []));
        using var chatFactory = CreateFactory(model);
        using var client = CreateClient(chatFactory);
        var csrfToken = await LoginAsync(client, "User1");

        var session = await CreateSessionAsync(client, csrfToken);
        using var messageRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/chat/sessions/{session.SessionId}/messages")
        {
            Content = JsonContent.Create(new { message = "Hola" })
        };
        messageRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);
        using var messageResponse = await client.SendAsync(messageRequest);
        var message = await messageResponse.Content.ReadFromJsonAsync<ChatMessageApiResponse>();
        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/chat/sessions/{session.SessionId}");
        deleteRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);
        using var deleteResponse = await client.SendAsync(deleteRequest);

        messageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        message.Should().NotBeNull();
        message!.AssistantMessage.Should().Be("Puedo ayudarte con las reservas.");
        message.Effects.Should().BeEmpty();
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        model.CallCount.Should().Be(1);
    }

    [TestMethod]
    public async Task AnotherUserCannotAccessChatSession()
    {
        var model = new ApiFakeChatModel();
        using var chatFactory = CreateFactory(model);
        using var user1Client = CreateClient(chatFactory);
        var user1CsrfToken = await LoginAsync(user1Client, "User1");
        var session = await CreateSessionAsync(user1Client, user1CsrfToken);
        using var user2Client = CreateClient(chatFactory);
        var user2CsrfToken = await LoginAsync(user2Client, "User2");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/chat/sessions/{session.SessionId}/messages")
        {
            Content = JsonContent.Create(new { message = "Mostrame la conversación" })
        };
        request.Headers.Add("X-CSRF-TOKEN", user2CsrfToken);

        using var response = await user2Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        model.CallCount.Should().Be(0);
    }

    [TestMethod]
    public async Task SendMessageRejectsMissingCsrfToken()
    {
        var model = new ApiFakeChatModel();
        using var chatFactory = CreateFactory(model);
        using var client = CreateClient(chatFactory);
        var csrfToken = await LoginAsync(client, "User1");
        var session = await CreateSessionAsync(client, csrfToken);

        using var response = await client.PostAsJsonAsync(
            $"/api/chat/sessions/{session.SessionId}/messages",
            new { message = "Hola" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        model.CallCount.Should().Be(0);
    }

    [TestMethod]
    public async Task DeleteSessionRejectsMissingCsrfToken()
    {
        var model = new ApiFakeChatModel();
        using var chatFactory = CreateFactory(model);
        using var client = CreateClient(chatFactory);
        var csrfToken = await LoginAsync(client, "User1");
        var session = await CreateSessionAsync(client, csrfToken);

        using var response = await client.DeleteAsync(
            $"/api/chat/sessions/{session.SessionId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ProviderFailureMapsToServiceUnavailable()
    {
        var model = new ApiFakeChatModel(new ChatModelException("Unavailable."));
        using var chatFactory = CreateFactory(model);
        using var client = CreateClient(chatFactory);
        var csrfToken = await LoginAsync(client, "User1");
        var session = await CreateSessionAsync(client, csrfToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/chat/sessions/{session.SessionId}/messages")
        {
            Content = JsonContent.Create(new { message = "Hola" })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private WebApplicationFactory<Program> CreateFactory(IChatModel model)
    {
        return factory!.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatModel>();
                services.AddSingleton(model);
            }));
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> applicationFactory)
    {
        return applicationFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName)
    {
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { userName, password = ChallengePassword });
        loginResponse.EnsureSuccessStatusCode();

        var csrfToken = await client.GetFromJsonAsync<CsrfTokenResponse>("/api/auth/csrf");
        return csrfToken!.Token;
    }

    private static async Task<ChatSessionApiResponse> CreateSessionAsync(
        HttpClient client,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat/sessions");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ChatSessionApiResponse>())!;
    }

    private sealed record CsrfTokenResponse(string Token);

    private sealed record ChatSessionApiResponse(string SessionId);

    private sealed record ChatMessageApiResponse(
        string AssistantMessage,
        IReadOnlyList<string> Effects);

    private sealed class ApiFakeChatModel : IChatModel
    {
        private readonly Queue<Func<ChatModelResponse>> responses = new();

        public ApiFakeChatModel(params ChatModelResponse[] responses)
        {
            foreach (var response in responses)
            {
                this.responses.Enqueue(() => response);
            }
        }

        public ApiFakeChatModel(ChatModelException exception)
        {
            responses.Enqueue(() => throw exception);
        }

        public int CallCount { get; private set; }

        public Task<ChatModelResponse> SendAsync(
            IReadOnlyList<ChatMessage> messages,
            IReadOnlyList<ChatToolDefinition> tools,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (responses.Count == 0)
            {
                throw new InvalidOperationException("No API fake response was configured.");
            }

            return Task.FromResult(responses.Dequeue().Invoke());
        }
    }
}
