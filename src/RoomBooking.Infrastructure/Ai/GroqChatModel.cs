using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RoomBooking.Application.Agent;

namespace RoomBooking.Infrastructure.Ai;

internal sealed class GroqChatModel : IChatModel
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly AiOptions options;
    private readonly HttpClient httpClient;

    public GroqChatModel(HttpClient httpClient, IOptions<AiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        this.options = options.Value;
        this.httpClient = httpClient;
    }

    public async Task<ChatModelResponse> SendAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(tools);

        ValidateConfiguration();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildChatCompletionsUri());
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(
            CreateRequestBody(messages, tools),
            options: SerializerOptions);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ChatModelException(
                    $"Groq returned HTTP {(int)response.StatusCode}.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);

            return ParseResponse(document.RootElement);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ChatModelException("The Groq request timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new ChatModelException("The Groq request failed.", exception);
        }
        catch (JsonException exception)
        {
            throw new ChatModelException("Groq returned invalid JSON.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new ChatModelException("Groq returned an invalid response.", exception);
        }
    }

    private object CreateRequestBody(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools)
    {
        return new
        {
            model = options.Model,
            messages = messages.Select(CreateMessageBody).ToArray(),
            tools = tools.Select(CreateToolBody).ToArray(),
            tool_choice = "auto",
            parallel_tool_calls = false
        };
    }

    private static Dictionary<string, object?> CreateMessageBody(ChatMessage message)
    {
        var body = new Dictionary<string, object?>
        {
            ["role"] = ToProtocolRole(message.Role),
            ["content"] = message.Content
        };

        if (message.Role == ChatMessageRole.Tool)
        {
            body["tool_call_id"] = message.ToolCallId;
        }

        if (message.ToolCalls is { Count: > 0 })
        {
            body["tool_calls"] = message.ToolCalls.Select(call => new
            {
                id = call.Id,
                type = "function",
                function = new
                {
                    name = call.Name,
                    arguments = call.ArgumentsJson
                }
            }).ToArray();
        }

        return body;
    }

    private static object CreateToolBody(ChatToolDefinition tool)
    {
        using var document = JsonDocument.Parse(tool.ParametersJsonSchema);

        return new
        {
            type = "function",
            function = new
            {
                name = tool.Name,
                description = tool.Description,
                parameters = document.RootElement.Clone()
            }
        };
    }

    private static ChatModelResponse ParseResponse(JsonElement root)
    {
        var choices = root.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("The response contains no choices.");
        }

        var message = choices[0].GetProperty("message");
        var content = message.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.String
                ? contentElement.GetString()
                : null;

        var toolCalls = new List<ChatToolCall>();
        if (message.TryGetProperty("tool_calls", out var toolCallsElement)
            && toolCallsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var toolCallElement in toolCallsElement.EnumerateArray())
            {
                var function = toolCallElement.GetProperty("function");
                toolCalls.Add(new ChatToolCall(
                    toolCallElement.GetProperty("id").GetString() ?? string.Empty,
                    function.GetProperty("name").GetString() ?? string.Empty,
                    function.GetProperty("arguments").GetString() ?? string.Empty));
            }
        }

        return new ChatModelResponse(content, toolCalls);
    }

    private Uri BuildChatCompletionsUri()
    {
        return new Uri(
            $"{options.Endpoint.TrimEnd('/')}/chat/completions",
            UriKind.Absolute);
    }

    private void ValidateConfiguration()
    {
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _))
        {
            throw new ChatModelException("AI:Endpoint must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new ChatModelException("AI:Model must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ChatModelException("AI:ApiKey must be configured.");
        }
    }

    private static string ToProtocolRole(ChatMessageRole role)
    {
        return role switch
        {
            ChatMessageRole.System => "system",
            ChatMessageRole.User => "user",
            ChatMessageRole.Assistant => "assistant",
            ChatMessageRole.Tool => "tool",
            _ => throw new InvalidOperationException($"Unsupported chat role: {role}.")
        };
    }
}
