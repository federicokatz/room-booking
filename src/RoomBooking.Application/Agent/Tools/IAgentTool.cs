namespace RoomBooking.Application.Agent.Tools;

public interface IAgentTool
{
    ChatToolDefinition Definition { get; }

    Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default);
}

public sealed record AgentToolResult(string ContentJson, string? Effect = null);
