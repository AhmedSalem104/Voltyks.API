using System;

namespace Voltyks.Persistence.Utilities
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo EgyptTimeZone = BuildEgyptTimeZone();

        /// <summary>
        /// Returns the current time in Egypt timezone.
        /// Winter (no DST): UTC+2. Summer (last Friday of April → last Thursday of October): UTC+3.
        /// The returned DateTime has <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        public static DateTime GetEgyptTime() =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EgyptTimeZone);

        /// <summary>
        /// Converts a UTC DateTime to Egypt timezone.
        /// </summary>
        public static DateTime ToEgyptTime(DateTime utc)
        {
            var utcValue = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utcValue, EgyptTimeZone);
        }

        /// <summary>
        /// Returns the Egypt UTC offset at the given UTC moment (+02:00 or +03:00 during DST).
        /// </summary>
        public static TimeSpan GetEgyptUtcOffset(DateTime atUtc) =>
            EgyptTimeZone.GetUtcOffset(atUtc);

        private static TimeZoneInfo BuildEgyptTimeZone()
        {
            // Try system timezones first — works on Windows with correct tzdata and on Linux/Azure.
            // Verify the base offset is actually +02:00 because some misconfigured hosts (e.g.
            // Monster ASP) return a zero-offset zone for "Egypt Standard Time".
            var expected = TimeSpan.FromHours(2);

            try
            {
                var sys = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                if (sys.BaseUtcOffset == expected) return sys;
            }
            catch { /* not available, fall through */ }

            try
            {
                var sys = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
                if (sys.BaseUtcOffset == expected) return sys;
            }
            catch { /* not available, fall through */ }

            // Fallback: build Egypt TZ explicitly with current DST rules (as of 2023).
            // DST starts: last Friday of April at 00:00 (spring forward to 01:00).
            // DST ends:   last Thursday of October at 23:59:59 (fall back).
            var dstDelta = TimeSpan.FromHours(1);
            var dstStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                timeOfDay: new DateTime(1, 1, 1, 0, 0, 0),
                month: 4, week: 5, dayOfWeek: DayOfWeek.Friday);
            var dstEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                timeOfDay: new DateTime(1, 1, 1, 23, 59, 59),
                month: 10, week: 5, dayOfWeek: DayOfWeek.Thursday);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                dateStart: DateTime.MinValue.Date,
                dateEnd: DateTime.MaxValue.Date,
                daylightDelta: dstDelta,
                daylightTransitionStart: dstStart,
                daylightTransitionEnd: dstEnd);

            return TimeZoneInfo.CreateCustomTimeZone(
                id: "Egypt (custom)",
                baseUtcOffset: expected,
                displayName: "(UTC+02:00) Egypt",
                standardDisplayName: "Egypt Standard Time",
                daylightDisplayName: "Egypt Daylight Time",
                adjustmentRules: new[] { adjustment });
        }
    }
}
