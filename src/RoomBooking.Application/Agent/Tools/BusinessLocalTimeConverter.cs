using RoomBooking.Application.Abstractions.Time;

namespace RoomBooking.Application.Agent.Tools;

internal static class BusinessLocalTimeConverter
{
    public static bool TryConvertToUtc(
        DateTime startLocal,
        DateTime endLocal,
        IBusinessTimeZone businessTimeZone,
        out DateTimeOffset startUtc,
        out DateTimeOffset endUtc)
    {
        ArgumentNullException.ThrowIfNull(businessTimeZone);

        startUtc = default;
        endUtc = default;

        if (startLocal.Kind != DateTimeKind.Unspecified
            || endLocal.Kind != DateTimeKind.Unspecified
            || businessTimeZone.Value.IsInvalidTime(startLocal)
            || businessTimeZone.Value.IsInvalidTime(endLocal)
            || businessTimeZone.Value.IsAmbiguousTime(startLocal)
            || businessTimeZone.Value.IsAmbiguousTime(endLocal))
        {
            return false;
        }

        startUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(startLocal, businessTimeZone.Value));
        endUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(endLocal, businessTimeZone.Value));
        return true;
    }

    public static DateTime ConvertToLocal(
        DateTimeOffset utcValue,
        IBusinessTimeZone businessTimeZone)
    {
        ArgumentNullException.ThrowIfNull(businessTimeZone);

        return TimeZoneInfo.ConvertTime(utcValue, businessTimeZone.Value).DateTime;
    }
}
