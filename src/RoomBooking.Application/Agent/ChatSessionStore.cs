using System.Collections.Concurrent;

namespace RoomBooking.Application.Agent;

public sealed class ChatSessionStore(TimeProvider timeProvider)
{
    public const int MaximumHistoryMessages = 20;

    public static TimeSpan SlidingExpiration { get; } = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, ChatSession> sessions =
        new(StringComparer.Ordinal);

    public string Create(string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        while (true)
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var session = new ChatSession(owner, timeProvider.GetUtcNow());

            if (sessions.TryAdd(sessionId, session))
            {
                return sessionId;
            }
        }
    }

    public bool Delete(string sessionId, string owner)
    {
        if (!TryGetOwnedSession(sessionId, owner, out _))
        {
            return false;
        }

        return sessions.TryRemove(sessionId, out _);
    }

    internal async Task<ChatSessionExecutionResult<T>> ExecuteAsync<T>(
        string sessionId,
        string owner,
        Func<List<ChatMessage>, Task<T>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!TryGetOwnedSession(sessionId, owner, out var session))
        {
            return ChatSessionExecutionResult<T>.NotFound();
        }

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (now - session.LastAccessUtc >= SlidingExpiration)
            {
                sessions.TryRemove(sessionId, out _);
                return ChatSessionExecutionResult<T>.NotFound();
            }

            session.LastAccessUtc = now;
            var value = await action(session.History);
            TrimHistory(session.History);
            session.LastAccessUtc = timeProvider.GetUtcNow();

            return ChatSessionExecutionResult<T>.Found(value);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private bool TryGetOwnedSession(
        string sessionId,
        string owner,
        out ChatSession session)
    {
        session = null!;

        if (string.IsNullOrWhiteSpace(sessionId)
            || string.IsNullOrWhiteSpace(owner)
            || !sessions.TryGetValue(sessionId, out var existing)
            || !string.Equals(existing.Owner, owner, StringComparison.Ordinal))
        {
            return false;
        }

        if (timeProvider.GetUtcNow() - existing.LastAccessUtc >= SlidingExpiration)
        {
            sessions.TryRemove(sessionId, out _);
            return false;
        }

        session = existing;
        return true;
    }

    internal static void TrimHistory(List<ChatMessage> history)
    {
        if (history.Count <= MaximumHistoryMessages)
        {
            return;
        }

        history.RemoveRange(0, history.Count - MaximumHistoryMessages);

        while (history.Count > 0 && history[0].Role == ChatMessageRole.Tool)
        {
            history.RemoveAt(0);
        }
    }

    internal sealed class ChatSession(string owner, DateTimeOffset createdAtUtc)
    {
        public string Owner { get; } = owner;

        public DateTimeOffset LastAccessUtc { get; set; } = createdAtUtc;

        public List<ChatMessage> History { get; } = [];

        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}

internal sealed record ChatSessionExecutionResult<T>(bool Exists, T? Value)
{
    public static ChatSessionExecutionResult<T> Found(T value)
    {
        return new ChatSessionExecutionResult<T>(true, value);
    }

    public static ChatSessionExecutionResult<T> NotFound()
    {
        return new ChatSessionExecutionResult<T>(false, default);
    }
}
