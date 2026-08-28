using RoomBooking.Application.Agent;
using RoomBooking.Application.Agent.Tools;

namespace RoomBooking.Application.Tests.Fakes;

internal sealed class FakeAgentTool(
    string name,
    AgentToolResult result,
    bool isMutation = false,
    Exception? exception = null) : IAgentTool
{
    public ChatToolDefinition Definition { get; } = new(
        name,
        "Fake tool for deterministic tests.",
        "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}",
        isMutation);

    public int ExecutionCount { get; private set; }

    public string? LastArgumentsJson { get; private set; }

    public Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        LastArgumentsJson = argumentsJson;

        return exception is null
            ? Task.FromResult(result)
            : Task.FromException<AgentToolResult>(exception);
    }
}
