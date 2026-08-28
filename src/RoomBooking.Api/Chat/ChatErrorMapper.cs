using RoomBooking.Domain.Common;

namespace RoomBooking.Api.Chat;

internal static class ChatErrorMapper
{
    public static IResult ToProblem(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = error.Code switch
        {
            "authentication.required" => StatusCodes.Status401Unauthorized,
            "chat.session_not_found" => StatusCodes.Status404NotFound,
            "chat.provider_unavailable" => StatusCodes.Status503ServiceUnavailable,
            "chat.invalid_model_response" => StatusCodes.Status502BadGateway,
            "chat.execution_limit_reached" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            statusCode: statusCode,
            title: "Chat request failed",
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code
            });
    }
}
