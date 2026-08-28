using Microsoft.AspNetCore.Antiforgery;

namespace RoomBooking.Api.Security;

internal static class AntiforgeryValidationExtensions
{
    public static RouteHandlerBuilder RequireValidAntiforgeryToken(
        this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddEndpointFilter(async (context, next) =>
        {
            var antiforgery = context.HttpContext.RequestServices
                .GetRequiredService<IAntiforgery>();

            try
            {
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest();
            }

            return await next(context);
        });
    }
}
