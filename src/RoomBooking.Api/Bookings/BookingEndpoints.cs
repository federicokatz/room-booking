using RoomBooking.Api.Security;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Bookings.CancelBooking;
using RoomBooking.Application.Bookings.CreateBooking;
using RoomBooking.Application.Bookings.GetRoomSchedule;
using RoomBooking.Application.Bookings.ListAvailableRooms;
using RoomBooking.Application.Bookings.ListMyBookings;
using RoomBooking.Application.Rooms;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Common;

namespace RoomBooking.Api.Bookings;

internal static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var rooms = endpoints.MapGroup("/api/rooms").RequireAuthorization();
        rooms.MapGet("", ListRoomsAsync);
        rooms.MapGet("/available", ListAvailableRoomsAsync);
        rooms.MapGet("/{roomCode}/schedule", GetRoomScheduleAsync);

        var bookings = endpoints.MapGroup("/api/bookings").RequireAuthorization();
        bookings.MapPost("", CreateBookingAsync).RequireValidAntiforgeryToken();
        bookings.MapGet("/mine", ListMyBookingsAsync);
        bookings.MapPost("/{bookingId:guid}/cancel", CancelBookingAsync)
            .RequireValidAntiforgeryToken();

        return endpoints;
    }

    private static async Task<IResult> ListRoomsAsync(
        ListRoomsUseCase useCase,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await useCase.ExecuteAsync(cancellationToken));
    }

    private static async Task<IResult> ListAvailableRoomsAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        int attendees,
        ListAvailableRoomsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new ListAvailableRoomsQuery(startUtc, endUtc, attendees),
            cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetRoomScheduleAsync(
        string roomCode,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        GetRoomScheduleUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new GetRoomScheduleQuery(roomCode, startUtc, endUtc),
            cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);
    }

    private static async Task<IResult> CreateBookingAsync(
        CreateBookingRequest request,
        CreateBookingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new CreateBookingCommand(
                request.RoomCode,
                request.StartUtc,
                request.EndUtc,
                request.Title,
                request.Attendees),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/bookings/{result.Value.Id}", result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> ListMyBookingsAsync(
        ListMyBookingsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);
    }

    private static async Task<IResult> CancelBookingAsync(
        Guid bookingId,
        CancelBookingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(bookingId, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);
    }

    private static IResult ToProblem(DomainError error)
    {
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

    private sealed record CreateBookingRequest(
        string? RoomCode,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        string? Title,
        int Attendees);
}
