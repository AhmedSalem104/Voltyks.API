using System;

namespace Voltyks.Core.Utilities
{
    public static class MoneyRounding
    {
        public static int ToInt(decimal value) =>
            (int)Math.Round(value, 0, MidpointRounding.AwayFromZero);

        public static int ToInt(double value) =>
            (int)Math.Round(value, 0, MidpointRounding.AwayFromZero);

        public static int? ToIntOrNull(decimal? value) =>
            value.HasValue ? ToInt(value.Value) : (int?)null;

        public static int? ToIntOrNull(double? value) =>
            value.HasValue ? ToInt(value.Value) : (int?)null;
    }
}
