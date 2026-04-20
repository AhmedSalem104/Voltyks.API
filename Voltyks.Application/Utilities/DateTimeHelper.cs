using System;

namespace Voltyks.Application.Utilities
{
    /// <summary>
    /// Forwarder to <see cref="Voltyks.Persistence.Utilities.DateTimeHelper"/>.
    /// The canonical helper lives in Persistence so entities can use it too.
    /// </summary>
    public static class DateTimeHelper
    {
        public static DateTime GetEgyptTime() =>
            Voltyks.Persistence.Utilities.DateTimeHelper.GetEgyptTime();

        public static DateTime ToEgyptTime(DateTime utc) =>
            Voltyks.Persistence.Utilities.DateTimeHelper.ToEgyptTime(utc);

        public static TimeSpan GetEgyptUtcOffset(DateTime atUtc) =>
            Voltyks.Persistence.Utilities.DateTimeHelper.GetEgyptUtcOffset(atUtc);
    }
}
