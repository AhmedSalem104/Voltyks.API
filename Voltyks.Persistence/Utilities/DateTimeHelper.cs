using System;

namespace Voltyks.Persistence.Utilities
{
    /// <summary>
    /// Egypt timezone helper using pure arithmetic — no dependency on
    /// TimeZoneInfo or system tzdata (which is broken on Monster ASP hosting).
    /// Egypt uses UTC+2 in winter and UTC+3 during DST.
    /// DST rule: last Friday of April (00:00 local) → last Thursday of October (24:00 local).
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeSpan StandardOffset = TimeSpan.FromHours(2);
        private static readonly TimeSpan DaylightOffset = TimeSpan.FromHours(3);

        /// <summary>
        /// Returns the current Egypt wall-clock time.
        /// The returned DateTime has <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        public static DateTime GetEgyptTime()
        {
            var utc = DateTime.UtcNow;
            var offset = GetOffsetForUtc(utc);
            return DateTime.SpecifyKind(utc.Add(offset), DateTimeKind.Unspecified);
        }

        /// <summary>Convert a UTC DateTime to Egypt wall-clock time.</summary>
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

        private static TimeSpan GetOffsetForUtc(DateTime utc)
        {
            return IsEgyptDstActive(utc) ? DaylightOffset : StandardOffset;
        }

        /// <summary>
        /// DST starts at 00:00 Egypt (last Friday of April) = previous day 22:00 UTC.
        /// DST ends at 24:00 Egypt DST (last Thursday of October) = same day 21:00 UTC.
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
