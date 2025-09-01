using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Voltyks.Application.Services.Paymob
{
    public static class HttpClientRetryExtensions
    {
        public static async Task<HttpResponseMessage> PostJsonWithRetryAsync(
            this HttpClient client, string url, object body, int maxRetries = 3,
            ILogger? log = null, CancellationToken ct = default)
        {
            for (int attempt = 0; ; attempt++)
            {
                var res = await client.PostAsJsonAsync(url, body, ct);
                if (res.StatusCode != (HttpStatusCode)429) return res;

                if (attempt >= maxRetries) return res;

                var delay = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Min(8, 2 * (attempt + 1)));
                log?.LogWarning("429 from {Url}. Retrying in {Delay}s (attempt {Attempt}/{Max})",
                    url, delay.TotalSeconds, attempt + 1, maxRetries);
                await Task.Delay(delay, ct);
            }
        }

        public static async Task<HttpResponseMessage> SendWithRetryAsync(
            this HttpClient client, HttpRequestMessage req, int maxRetries = 3,
            ILogger? log = null, CancellationToken ct = default)
        {
            for (int attempt = 0; ; attempt++)
            {
                var res = await client.SendAsync(req, ct);
                if (res.StatusCode != (HttpStatusCode)429) return res;

                if (attempt >= maxRetries) return res;

                var delay = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Min(8, 2 * (attempt + 1)));
                log?.LogWarning("429 from {Url}. Retrying in {Delay}s (attempt {Attempt}/{Max})",
                    req.RequestUri, delay.TotalSeconds, attempt + 1, maxRetries);
                await Task.Delay(delay, ct);
            }
        }
    }
}
