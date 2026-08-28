using RoomBooking.Domain.Common;

namespace RoomBooking.Api.Bookings;

internal static class BookingErrorMapper
{
    public static IResult ToProblem(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = error.Code switch
        {
            "authentication.required" => StatusCodes.Status401Unauthorized,
            "room.not_found" or "booking.not_found" => StatusCodes.Status404NotFound,
            "booking.not_owner" => StatusCodes.Status403Forbidden,
            "booking.overlap" or "booking.already_cancelled" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            statusCode: statusCode,
            title: "Booking request failed",
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code
            });
    }
}
