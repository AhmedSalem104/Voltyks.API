using System;

namespace Voltyks.Persistence.Utilities
{
    /// <summary>
    /// Timestamp helper — stored values are UTC across the app. The JSON
    /// converter in the API layer handles the UTC → Egypt conversion at
    /// serialization time, so clients still see Egypt wall-clock time with
    /// the matching offset (+02:00 / +03:00 during DST).
    ///
    /// All fields we persist to the database must be UTC, otherwise any
    /// internal comparison (<c>DateTime.UtcNow - stored</c>) breaks.
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeSpan StandardOffset = TimeSpan.FromHours(2);
        private static readonly TimeSpan DaylightOffset = TimeSpan.FromHours(3);

        /// <summary>
        /// Returns <c>DateTime.UtcNow</c>. Kept under this name for
        /// backwards compatibility — callers can continue to use
        /// <c>DateTimeHelper.GetEgyptTime()</c> everywhere and end up
        /// with consistent UTC-stored values. The JSON converter on the
        /// way out is the single place that converts to Egypt time.
        /// </summary>
        public static DateTime GetEgyptTime() => DateTime.UtcNow;

        /// <summary>
        /// Converts a UTC DateTime to Egypt wall-clock time
        /// (used by the JSON converter, not for persistence).
        /// </summary>
        public static DateTime ToEgyptTime(DateTime utc)
        {
            var utcValue = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            var offset = GetOffsetForUtc(utcValue);
            return DateTime.SpecifyKind(utcValue.Add(offset), DateTimeKind.Unspecified);
        }

        /// <summary>Returns +02:00 in winter, +03:00 during Egypt DST.</summary>
        public static TimeSpan GetEgyptUtcOffset(DateTime atUtc)
        {
            var utc = atUtc.Kind == DateTimeKind.Utc ? atUtc : DateTime.SpecifyKind(atUtc, DateTimeKind.Utc);
            return GetOffsetForUtc(utc);
        }

        /// <summary>
        /// Serializes a UTC-stored DateTime as an ISO 8601 string with the
        /// Egypt offset applied (e.g. "2026-04-23T13:23:11.6869846+02:00").
        /// Use for wire formats that bypass the JSON converter — timer
        /// strings inside FCM extraData dictionaries, SignalR payloads,
        /// uiContext fields, etc.
        /// </summary>
        public static string ToEgyptIsoString(DateTime utc)
        {
            var u = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            var offset = GetOffsetForUtc(u);
            return new DateTimeOffset(u).ToOffset(offset).ToString("o");
        }

        private static TimeSpan GetOffsetForUtc(DateTime utc)
        {
            return IsEgyptDstActive(utc) ? DaylightOffset : StandardOffset;
        }

        /// <summary>
        /// Egypt DST window: last Friday of April 00:00 local → last Thursday of October 24:00 local.
        /// </summary>
        private static bool IsEgyptDstActive(DateTime utc)
        {
            var year = utc.Year;

            var dstStartLocal = LastWeekdayOfMonth(year, 4, DayOfWeek.Friday);
            var dstStartUtc = new DateTime(dstStartLocal.Year, dstStartLocal.Month, dstStartLocal.Day, 0, 0, 0, DateTimeKind.Utc)
                .AddHours(-2); // 00:00 Egypt (UTC+2) == 22:00 UTC previous day

            var dstEndLocal = LastWeekdayOfMonth(year, 10, DayOfWeek.Thursday);
            var dstEndUtc = new DateTime(dstEndLocal.Year, dstEndLocal.Month, dstEndLocal.Day, 0, 0, 0, DateTimeKind.Utc)
                .AddDays(1).AddHours(-3); // 24:00 Egypt DST (UTC+3) == 21:00 UTC

            return utc >= dstStartUtc && utc < dstEndUtc;
        }

        private static DateTime LastWeekdayOfMonth(int year, int month, DayOfWeek day)
        {
            var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            while (last.DayOfWeek != day) last = last.AddDays(-1);
            return last;
        }
    }
}
