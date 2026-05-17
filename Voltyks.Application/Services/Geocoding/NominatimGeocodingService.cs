using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Voltyks.Application.Interfaces.Geocoding;

namespace Voltyks.Application.Services.Geocoding
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private const string NotAvailable = "N/A";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
        private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NominatimGeocodingService> _logger;

        public NominatimGeocodingService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<NominatimGeocodingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(string Area, string Street)> GetAddressAsync(
            double latitude, double longitude, CancellationToken ct = default)
        {
            var cacheKey = BuildCacheKey(latitude, longitude);

            if (_cache.TryGetValue(cacheKey, out (string Area, string Street) cached))
                return cached;

            var result = await CallNominatimAsync(latitude, longitude, ct);

            if (result.Area != NotAvailable || result.Street != NotAvailable)
                _cache.Set(cacheKey, result, CacheTtl);

            return result;
        }

        private async Task<(string Area, string Street)> CallNominatimAsync(
            double latitude, double longitude, CancellationToken ct)
        {
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={0}&lon={1}&addressdetails=1&accept-language=ar",
                latitude, longitude);

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = HttpTimeout;
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("VoltyksApp", "1.0"));
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("(support@voltyks.com)"));

                var resp = await client.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Nominatim geocoding non-success status {StatusCode} for (lat={Lat}, lng={Lng})",
                        (int)resp.StatusCode, latitude, longitude);
                    return (NotAvailable, NotAvailable);
                }

                var body = await resp.Content.ReadAsStringAsync(ct);
                var json = JObject.Parse(body);
                var address = json["address"] as JObject;
                if (address == null)
                {
                    _logger.LogWarning(
                        "Nominatim geocoding returned no address object for (lat={Lat}, lng={Lng})",
                        latitude, longitude);
                    return (NotAvailable, NotAvailable);
                }

                string street =
                    (string)address["road"] ??
                    (string)address["pedestrian"] ??
                    (string)address["footway"] ??
                    (string)address["path"] ??
                    (string)address["residential"] ??
                    (string)address["neighbourhood"] ??
                    NotAvailable;

                string area =
                    (string)address["suburb"] ??
                    (string)address["neighbourhood"] ??
                    (string)address["city_district"] ??
                    (string)address["city"] ??
                    (string)address["town"] ??
                    (string)address["village"] ??
                    (string)address["county"] ??
                    (string)address["state_district"] ??
                    (string)address["state"] ??
                    NotAvailable;

                return (area, street);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex,
                    "Nominatim geocoding timed out for (lat={Lat}, lng={Lng})",
                    latitude, longitude);
                return (NotAvailable, NotAvailable);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex,
                    "Nominatim geocoding HTTP error for (lat={Lat}, lng={Lng})",
                    latitude, longitude);
                return (NotAvailable, NotAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Nominatim geocoding unexpected error for (lat={Lat}, lng={Lng})",
                    latitude, longitude);
                return (NotAvailable, NotAvailable);
            }
        }

        private static string BuildCacheKey(double latitude, double longitude) =>
            string.Format(CultureInfo.InvariantCulture, "geo:{0:F6}:{1:F6}", latitude, longitude);
    }
}
