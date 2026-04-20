namespace Voltyks.Core.Constants
{
    public static class Languages
    {
        public const string En = "en";
        public const string Ar = "ar";
        public const string Default = En;

        /// <summary>
        /// Returns "ar" if the input is Arabic, otherwise falls back to "en".
        /// Handles null, whitespace, and unknown values.
        /// </summary>
        public static string Normalize(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) return Default;
            var lower = lang.Trim().ToLowerInvariant();
            return lower == Ar ? Ar : En;
        }
    }
}
