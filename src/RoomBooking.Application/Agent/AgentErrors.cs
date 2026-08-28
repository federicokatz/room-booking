using RoomBooking.Domain.Common;

namespace RoomBooking.Application.Agent;

public static class AgentErrors
{
    public static DomainError NotAuthenticated { get; } = new(
        "authentication.required",
        "An authenticated user is required.");

    public static DomainError SessionNotFound { get; } = new(
        "chat.session_not_found",
        "The chat session was not found.");

    public static DomainError MessageRequired { get; } = new(
        "chat.message_required",
        "A chat message is required.");

    public static DomainError ProviderUnavailable { get; } = new(
        "chat.provider_unavailable",
        "The assistant provider is temporarily unavailable.");

    public static DomainError InvalidModelResponse { get; } = new(
        "chat.invalid_model_response",
        "The assistant returned an invalid response.");

    public static DomainError ExecutionLimitReached { get; } = new(
        "chat.execution_limit_reached",
        "The assistant could not complete the request within the execution limit.");
}
