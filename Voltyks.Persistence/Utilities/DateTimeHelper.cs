using System;

namespace Voltyks.Persistence.Utilities
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// Returns the current time in Egypt timezone (UTC+2, or UTC+3 during DST).
        /// The returned DateTime has <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        public static DateTime GetEgyptTime()
        {
            var egyptZone = GetEgyptTimeZone();
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptZone);
        }

        /// <summary>
        /// Converts any DateTime (assumed UTC when <see cref="DateTimeKind.Unspecified"/>)
        /// to Egypt timezone.
        /// </summary>
        public static DateTime ToEgyptTime(DateTime utc)
        {
            var egyptZone = GetEgyptTimeZone();
            var utcValue = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utcValue, egyptZone);
        }

        /// <summary>
        /// Returns the Egypt UTC offset as a <see cref="TimeSpan"/> for the given moment.
        /// </summary>
        public static TimeSpan GetEgyptUtcOffset(DateTime atUtc)
        {
            return GetEgyptTimeZone().GetUtcOffset(atUtc);
        }

        private static TimeZoneInfo GetEgyptTimeZone()
        {
            // "Egypt Standard Time" on Windows, "Africa/Cairo" on Linux/macOS
            try { return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); }
        }
    }
}
