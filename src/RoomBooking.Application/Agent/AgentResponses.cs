namespace RoomBooking.Application.Agent;

public sealed record CreateChatSessionResponse(string SessionId);

public sealed record SendChatMessageResponse(
    string AssistantMessage,
    IReadOnlyList<string> Effects);
