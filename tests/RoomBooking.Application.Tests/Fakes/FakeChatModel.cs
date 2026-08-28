using RoomBooking.Application.Agent;

namespace RoomBooking.Application.Tests.Fakes;

internal sealed class FakeChatModel : IChatModel
{
    private readonly Queue<Func<ChatModelResponse>> responses = new();

    public List<IReadOnlyList<ChatMessage>> Requests { get; } = [];

    public void Enqueue(ChatModelResponse response)
    {
        responses.Enqueue(() => response);
    }

    public void EnqueueException(ChatModelException exception)
    {
        responses.Enqueue(() => throw exception);
    }

    public Task<ChatModelResponse> SendAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(messages.ToArray());

        if (responses.Count == 0)
        {
            throw new InvalidOperationException("No fake chat-model response was configured.");
        }

        return Task.FromResult(responses.Dequeue().Invoke());
    }
}
