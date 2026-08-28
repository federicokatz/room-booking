using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoomBooking.Domain.Common;

namespace RoomBooking.Application.Agent.Tools;

internal static class AgentToolJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    static AgentToolJson()
    {
        SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static bool TryDeserialize<T>(
        string json,
        [NotNullWhen(true)] out T? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
        catch (NotSupportedException)
        {
            value = default;
            return false;
        }
    }

    public static AgentToolResult Success<T>(T value, string? effect = null)
    {
        return new AgentToolResult(
            JsonSerializer.Serialize(new ToolSuccessResponse<T>(true, value), SerializerOptions),
            effect);
    }

    public static AgentToolResult Failure(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return Failure(error.Code, error.Description);
    }

    public static AgentToolResult Failure(string code, string message)
    {
        return new AgentToolResult(
            JsonSerializer.Serialize(new ToolFailureResponse(false, code, message), SerializerOptions));
    }

    private sealed record ToolSuccessResponse<T>(bool Success, T Data);

    private sealed record ToolFailureResponse(bool Success, string Code, string Message);
}
