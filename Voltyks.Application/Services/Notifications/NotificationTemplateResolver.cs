using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Voltyks.Application.Interfaces.Notifications;
using Voltyks.Core.Constants;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services.Notifications
{
    public class NotificationTemplateResolver : INotificationTemplateResolver
    {
        private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled);
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        private readonly VoltyksDbContext _ctx;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NotificationTemplateResolver> _logger;

        public NotificationTemplateResolver(
            VoltyksDbContext ctx,
            IMemoryCache cache,
            ILogger<NotificationTemplateResolver> logger)
        {
            _ctx = ctx;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(string Title, string Body)> ResolveAsync(
            string key, string lang,
            IDictionary<string, string>? parameters,
            CancellationToken ct = default)
        {
            var normalizedLang = Languages.Normalize(lang);

            // 1) Cache check
            if (_cache.TryGetValue(CacheKey(key), out NotificationTemplate? cached) && cached != null)
                return Render(cached, normalizedLang, parameters);

            // 2) DB lookup (cache the result either way to avoid repeated lookups)
            NotificationTemplate? row = null;
            try
            {
                row = await _ctx.Set<NotificationTemplate>()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(t => t.Key == key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to read NotificationTemplate {Key} from DB, using hardcoded fallback",
                    key);
            }

            if (row != null)
            {
                _cache.Set(CacheKey(key), row, CacheTtl);
                return Render(row, normalizedLang, parameters);
            }

            // 3) Hardcoded fallback
            if (HardcodedTemplateRegistry.All.TryGetValue(key, out var entry))
                return entry.Render(normalizedLang, parameters);

            _logger.LogError("Unknown notification template key: {Key}", key);
            return ("Notification", "");
        }

        public void Invalidate(string key) => _cache.Remove(CacheKey(key));

        private static string CacheKey(string key) => $"notif:tpl:{key}";

        private static (string Title, string Body) Render(
            NotificationTemplate row,
            string lang,
            IDictionary<string, string>? parameters)
        {
            var title = lang == Languages.Ar ? row.TitleAr : row.TitleEn;
            var body = lang == Languages.Ar ? row.BodyAr : row.BodyEn;

            if (parameters == null || parameters.Count == 0)
                return (Substitute(title, parameters), Substitute(body, parameters));

            return (Substitute(title, parameters), Substitute(body, parameters));
        }

        private static string Substitute(string template, IDictionary<string, string>? parameters)
        {
            if (string.IsNullOrEmpty(template) || parameters == null || parameters.Count == 0)
                return template ?? "";

            return Placeholder.Replace(template, m =>
                parameters.TryGetValue(m.Groups[1].Value, out var v) ? v ?? "" : m.Value);
        }
    }
}
