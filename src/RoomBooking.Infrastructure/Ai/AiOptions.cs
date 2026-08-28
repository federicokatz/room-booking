namespace RoomBooking.Infrastructure.Ai;

internal sealed class AiOptions
{
    public const string SectionName = "AI";

    public const string DefaultEndpoint = "https://api.groq.com/openai/v1";
    public const string DefaultModel = "openai/gpt-oss-20b";

    public string Endpoint { get; init; } = DefaultEndpoint;

    public string Model { get; init; } = DefaultModel;

    public string ApiKey { get; init; } = string.Empty;
}
