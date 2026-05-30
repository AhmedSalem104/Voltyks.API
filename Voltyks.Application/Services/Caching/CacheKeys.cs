namespace Voltyks.Application.Services.Caching
{
    public static class CacheKeys
    {
        // Static data cache keys
        public const string Protocols = "protocols:all";
        public const string Capacities = "capacities:all";
        public const string PriceOptions = "prices:all";
        public const string Brands = "brands:all";
        public const string ComplaintCategories = "complaint:categories";
        public const string FeesConfig = "fees:config";

        // Dynamic data cache keys with patterns
        public static string ModelsByBrand(int brandId) => $"models:brand:{brandId}";
        public static string UserById(string userId) => $"user:{userId}";

        // Cache durations
        public static class Duration
        {
            public static readonly TimeSpan OneHour = TimeSpan.FromHours(1);
            public static readonly TimeSpan ThirtyMinutes = TimeSpan.FromMinutes(30);
            public static readonly TimeSpan TenMinutes = TimeSpan.FromMinutes(10);
            public static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);
        }
    }
}
