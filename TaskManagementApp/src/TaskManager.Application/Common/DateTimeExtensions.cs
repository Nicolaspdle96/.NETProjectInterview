namespace TaskManager.Application.Common;

internal static class DateTimeExtensions
{
    /// <summary>
    /// Normalizes client-supplied dates: offsets are converted to UTC and values
    /// without any zone information (e.g. <c>"2026-10-01"</c>) are taken as UTC.
    /// </summary>
    public static DateTime? AsUtc(this DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } utc => utc,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        { } unspecified => DateTime.SpecifyKind(unspecified, DateTimeKind.Utc),
    };
}
