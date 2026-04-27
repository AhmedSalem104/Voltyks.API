using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voltyks.AdminControlDashboard.Dtos.Notifications;
using Voltyks.AdminControlDashboard.Interfaces.Notifications;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.Notifications;
using Voltyks.Application.Services.Notifications;
using Voltyks.Core.Constants;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Persistence.Utilities;

namespace Voltyks.AdminControlDashboard.Services.Notifications
{
    public class AdminNotificationCenterService : IAdminNotificationCenterService
    {
        private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled);
        private const int FcmBatchSize = 500;

        private readonly VoltyksDbContext _ctx;
        private readonly INotificationTemplateResolver _resolver;
        private readonly IFirebaseService _firebase;
        private readonly ILogger<AdminNotificationCenterService> _logger;

        public AdminNotificationCenterService(
            VoltyksDbContext ctx,
            INotificationTemplateResolver resolver,
            IFirebaseService firebase,
            ILogger<AdminNotificationCenterService> logger)
        {
            _ctx = ctx;
            _resolver = resolver;
            _firebase = firebase;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────────────
        // TEMPLATES
        // ──────────────────────────────────────────────────────────────────

        public async Task<ApiResponse<List<NotificationTemplateDto>>> ListTemplatesAsync(CancellationToken ct = default)
        {
            var dbRows = await _ctx.Set<NotificationTemplate>().AsNoTracking().ToListAsync(ct);
            var dbByKey = dbRows.ToDictionary(r => r.Key);

            var result = new List<NotificationTemplateDto>(HardcodedTemplateRegistry.All.Count);
            foreach (var (key, entry) in HardcodedTemplateRegistry.All)
            {
                if (dbByKey.TryGetValue(key, out var row))
                {
                    result.Add(new NotificationTemplateDto
                    {
                        Key = key,
                        TitleEn = row.TitleEn,
                        TitleAr = row.TitleAr,
                        BodyEn = row.BodyEn,
                        BodyAr = row.BodyAr,
                        RequiredParams = entry.RequiredParams.ToList(),
                        IsCustomized = true,
                        UpdatedAt = row.UpdatedAt,
                        UpdatedBy = row.UpdatedBy
                    });
                }
                else
                {
                    result.Add(new NotificationTemplateDto
                    {
                        Key = key,
                        TitleEn = entry.SampleEnTitle,
                        TitleAr = entry.SampleArTitle,
                        BodyEn = entry.SampleEnBody,
                        BodyAr = entry.SampleArBody,
                        RequiredParams = entry.RequiredParams.ToList(),
                        IsCustomized = false
                    });
                }
            }
            return new ApiResponse<List<NotificationTemplateDto>>(result, "Templates fetched", true);
        }

        public async Task<ApiResponse<NotificationTemplateDto>> GetTemplateAsync(string key, CancellationToken ct = default)
        {
            if (!HardcodedTemplateRegistry.All.TryGetValue(key, out var entry))
                return new ApiResponse<NotificationTemplateDto>(null!, $"Unknown template key: {key}", false);

            var row = await _ctx.Set<NotificationTemplate>().AsNoTracking().FirstOrDefaultAsync(t => t.Key == key, ct);

            var dto = row != null
                ? new NotificationTemplateDto
                {
                    Key = key,
                    TitleEn = row.TitleEn,
                    TitleAr = row.TitleAr,
                    BodyEn = row.BodyEn,
                    BodyAr = row.BodyAr,
                    RequiredParams = entry.RequiredParams.ToList(),
                    IsCustomized = true,
                    UpdatedAt = row.UpdatedAt,
                    UpdatedBy = row.UpdatedBy
                }
                : new NotificationTemplateDto
                {
                    Key = key,
                    TitleEn = entry.SampleEnTitle,
                    TitleAr = entry.SampleArTitle,
                    BodyEn = entry.SampleEnBody,
                    BodyAr = entry.SampleArBody,
                    RequiredParams = entry.RequiredParams.ToList(),
                    IsCustomized = false
                };

            return new ApiResponse<NotificationTemplateDto>(dto, "Template fetched", true);
        }

        public async Task<ApiResponse<NotificationTemplateDto>> UpdateTemplateAsync(
            string key, UpdateNotificationTemplateDto dto, string adminUserId, CancellationToken ct = default)
        {
            if (!HardcodedTemplateRegistry.All.TryGetValue(key, out var entry))
                return new ApiResponse<NotificationTemplateDto>(null!, $"Unknown template key: {key}", false);

            // Validate placeholders
            var required = entry.RequiredParams.ToHashSet();
            foreach (var field in new[] { dto.TitleEn, dto.TitleAr, dto.BodyEn, dto.BodyAr })
            {
                var unknown = ExtractPlaceholders(field).Where(p => !required.Contains(p)).ToList();
                if (unknown.Count > 0)
                    return new ApiResponse<NotificationTemplateDto>(null!,
                        $"Unknown placeholder(s): {string.Join(", ", unknown)}", false);
            }

            // Required placeholders must appear at least once across body+title (EN and AR each)
            var enText = $"{dto.TitleEn} {dto.BodyEn}";
            var arText = $"{dto.TitleAr} {dto.BodyAr}";
            var missingEn = required.Where(p => !enText.Contains($"{{{p}}}")).ToList();
            var missingAr = required.Where(p => !arText.Contains($"{{{p}}}")).ToList();
            if (missingEn.Count > 0 || missingAr.Count > 0)
            {
                var detail = new List<string>();
                if (missingEn.Count > 0) detail.Add($"EN missing: {string.Join(", ", missingEn)}");
                if (missingAr.Count > 0) detail.Add($"AR missing: {string.Join(", ", missingAr)}");
                return new ApiResponse<NotificationTemplateDto>(null!,
                    $"Required placeholder(s) not preserved. {string.Join(" | ", detail)}", false);
            }

            var existing = await _ctx.Set<NotificationTemplate>().FirstOrDefaultAsync(t => t.Key == key, ct);
            if (existing == null)
            {
                existing = new NotificationTemplate
                {
                    Key = key,
                    TitleEn = dto.TitleEn,
                    TitleAr = dto.TitleAr,
                    BodyEn = dto.BodyEn,
                    BodyAr = dto.BodyAr,
                    RequiredParamsJson = JsonSerializer.Serialize(entry.RequiredParams),
                    IsCustomizable = true,
                    UpdatedAt = DateTimeHelper.GetEgyptTime(),
                    UpdatedBy = adminUserId
                };
                _ctx.Add(existing);
            }
            else
            {
                existing.TitleEn = dto.TitleEn;
                existing.TitleAr = dto.TitleAr;
                existing.BodyEn = dto.BodyEn;
                existing.BodyAr = dto.BodyAr;
                existing.UpdatedAt = DateTimeHelper.GetEgyptTime();
                existing.UpdatedBy = adminUserId;
            }
            await _ctx.SaveChangesAsync(ct);
            _resolver.Invalidate(key);

            return await GetTemplateAsync(key, ct);
        }

        public async Task<ApiResponse<object>> ResetTemplateAsync(string key, CancellationToken ct = default)
        {
            if (!HardcodedTemplateRegistry.All.ContainsKey(key))
                return new ApiResponse<object>(null!, $"Unknown template key: {key}", false);

            var existing = await _ctx.Set<NotificationTemplate>().FirstOrDefaultAsync(t => t.Key == key, ct);
            if (existing != null)
            {
                _ctx.Remove(existing);
                await _ctx.SaveChangesAsync(ct);
            }
            _resolver.Invalidate(key);
            return new ApiResponse<object>(new { key, reset = true }, "Template reset to hardcoded default", true);
        }

        public async Task<ApiResponse<TemplatePreviewResultDto>> PreviewTemplateAsync(
            string key, TemplatePreviewRequestDto dto, CancellationToken ct = default)
        {
            if (!HardcodedTemplateRegistry.All.ContainsKey(key))
                return new ApiResponse<TemplatePreviewResultDto>(null!, $"Unknown template key: {key}", false);

            var fromDb = await _ctx.Set<NotificationTemplate>().AsNoTracking().AnyAsync(t => t.Key == key, ct);
            var (title, body) = await _resolver.ResolveAsync(key, Languages.Normalize(dto.Lang), dto.Params, ct);
            return new ApiResponse<TemplatePreviewResultDto>(
                new TemplatePreviewResultDto { Title = title, Body = body, FromDb = fromDb },
                "Preview rendered", true);
        }

        // ──────────────────────────────────────────────────────────────────
        // DISPATCH — single user
        // ──────────────────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> SendToUserAsync(SendToUserDto dto, string adminUserId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return new ApiResponse<object>(null!, "userId is required", false);

            var user = await _ctx.Set<AppUser>()
                .Include(u => u.DeviceTokens)
                .FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
            if (user == null)
                return new ApiResponse<object>(null!, "User not found", false);

            var lang = Languages.Normalize(user.PreferredLanguage);
            var (title, body, templateKey, validationError) = await BuildTitleBodyAsync(dto.Mode, dto.Template, dto.Custom, lang, ct);
            if (validationError != null)
                return new ApiResponse<object>(null!, validationError, false);

            // 1) DB row (mandatory)
            var notification = new Notification
            {
                UserId = user.Id,
                Title = title!,
                Body = body!,
                Type = templateKey ?? "Admin_Custom",
                IsAdminNotification = true,
                IsRead = false,
                SentAt = DateTimeHelper.GetEgyptTime()
            };
            _ctx.Add(notification);
            await _ctx.SaveChangesAsync(ct);

            // 2) FCM push (best-effort)
            var pushSent = 0;
            if (user.DeviceTokens != null && user.DeviceTokens.Any())
            {
                var extra = new Dictionary<string, string>
                {
                    ["NotificationType"] = templateKey ?? "Admin_Custom",
                    ["sentByAdmin"] = adminUserId
                };
                foreach (var t in user.DeviceTokens)
                {
                    try
                    {
                        await _firebase.SendNotificationAsync(t.Token, title!, body!, 0, templateKey ?? "Admin_Custom", extra);
                        pushSent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "FCM send-to-user failed. UserId={UserId}", user.Id);
                    }
                }
            }

            return new ApiResponse<object>(
                new { notificationId = notification.Id, userId = user.Id, title, body, pushSent },
                "Notification sent", true);
        }

        // ──────────────────────────────────────────────────────────────────
        // DISPATCH — broadcast
        // ──────────────────────────────────────────────────────────────────

        public async Task<ApiResponse<BroadcastResultDto>> BroadcastAsync(BroadcastDto dto, string adminUserId, CancellationToken ct = default)
        {
            // Resolve audience
            var query = _ctx.Set<AppUser>().AsQueryable().Where(u => !u.IsDeleted && !u.IsBanned);
            switch ((dto.Audience.Type ?? "all").ToLowerInvariant())
            {
                case "all":
                    break;
                case "role":
                    if (string.Equals(dto.Audience.Role, "charger_owner", StringComparison.OrdinalIgnoreCase))
                        query = query.Where(u => u.Chargers.Any());
                    else if (string.Equals(dto.Audience.Role, "vehicle_owner", StringComparison.OrdinalIgnoreCase))
                        query = query.Where(u => !u.Chargers.Any());
                    else
                        return new ApiResponse<BroadcastResultDto>(null!, "audience.role must be vehicle_owner or charger_owner", false);
                    break;
                case "users":
                    if (dto.Audience.UserIds == null || dto.Audience.UserIds.Count == 0)
                        return new ApiResponse<BroadcastResultDto>(null!, "audience.userIds is required when type=users", false);
                    var ids = dto.Audience.UserIds;
                    query = query.Where(u => ids.Contains(u.Id));
                    break;
                case "city":
                    return new ApiResponse<BroadcastResultDto>(null!, "audience.type=city is not supported yet", false);
                default:
                    return new ApiResponse<BroadcastResultDto>(null!, $"Unknown audience.type: {dto.Audience.Type}", false);
            }

            var recipients = await query
                .Select(u => new { u.Id, u.PreferredLanguage })
                .ToListAsync(ct);

            if (recipients.Count == 0)
                return new ApiResponse<BroadcastResultDto>(
                    new BroadcastResultDto(),
                    "No recipients matched the audience filter", true);

            // Pre-resolve content for each language so we don't render N times
            var enContent = await BuildTitleBodyAsync(dto.Mode, dto.Template, dto.Custom, Languages.En, ct);
            if (enContent.ValidationError != null)
                return new ApiResponse<BroadcastResultDto>(null!, enContent.ValidationError, false);
            var arContent = await BuildTitleBodyAsync(dto.Mode, dto.Template, dto.Custom, Languages.Ar, ct);

            var templateKey = enContent.TemplateKey;
            var notificationType = templateKey ?? "Admin_Broadcast";

            // Persist audit row + Notification rows + collect tokens
            var audit = new NotificationBroadcast
            {
                AdminUserId = adminUserId,
                AudienceJson = JsonSerializer.Serialize(dto.Audience),
                RecipientCount = recipients.Count,
                Title = enContent.Title!,
                Body = enContent.Body!,
                TemplateKey = templateKey,
                SentAt = DateTimeHelper.GetEgyptTime()
            };
            _ctx.Add(audit);
            await _ctx.SaveChangesAsync(ct);

            // Bulk-insert Notification rows (one per recipient, with their language-specific text)
            var sentAt = DateTimeHelper.GetEgyptTime();
            foreach (var r in recipients)
            {
                var lang = Languages.Normalize(r.PreferredLanguage);
                var (title, body) = lang == Languages.Ar ? (arContent.Title!, arContent.Body!) : (enContent.Title!, enContent.Body!);
                _ctx.Add(new Notification
                {
                    UserId = r.Id,
                    Title = title,
                    Body = body,
                    Type = notificationType,
                    IsAdminNotification = true,
                    IsRead = false,
                    SentAt = sentAt
                });
            }
            await _ctx.SaveChangesAsync(ct);
            audit.DbPersistedCount = recipients.Count;

            // FCM push (chunked per token across all recipients)
            var recipientIds = recipients.Select(r => r.Id).ToList();
            var tokens = await _ctx.Set<DeviceToken>()
                .AsNoTracking()
                .Where(t => recipientIds.Contains(t.UserId!) && !string.IsNullOrEmpty(t.Token))
                .Select(t => new { t.Token, t.UserId })
                .ToListAsync(ct);

            // Build language map per user
            var langByUser = recipients.ToDictionary(r => r.Id, r => Languages.Normalize(r.PreferredLanguage));

            audit.FcmAttemptedCount = tokens.Count;
            var fcmSucceeded = 0;

            for (int i = 0; i < tokens.Count; i += FcmBatchSize)
            {
                var chunk = tokens.Skip(i).Take(FcmBatchSize).ToList();
                var sends = chunk.Select(async tk =>
                {
                    var lang = langByUser.TryGetValue(tk.UserId!, out var l) ? l : Languages.En;
                    var (title, body) = lang == Languages.Ar ? (arContent.Title!, arContent.Body!) : (enContent.Title!, enContent.Body!);
                    try
                    {
                        await _firebase.SendNotificationAsync(tk.Token, title, body, 0, notificationType,
                            new Dictionary<string, string>
                            {
                                ["NotificationType"] = notificationType,
                                ["broadcastId"] = audit.Id.ToString(),
                                ["sentByAdmin"] = adminUserId
                            });
                        Interlocked.Increment(ref fcmSucceeded);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "FCM broadcast push failed. BroadcastId={BroadcastId} UserId={UserId}",
                            audit.Id, tk.UserId);
                    }
                });
                await Task.WhenAll(sends);
            }

            audit.FcmSucceededCount = fcmSucceeded;
            await _ctx.SaveChangesAsync(ct);

            return new ApiResponse<BroadcastResultDto>(
                new BroadcastResultDto
                {
                    BroadcastId = audit.Id,
                    RecipientCount = recipients.Count,
                    DbPersistedCount = audit.DbPersistedCount,
                    FcmAttemptedCount = audit.FcmAttemptedCount,
                    FcmSucceededCount = audit.FcmSucceededCount
                },
                "Broadcast dispatched", true);
        }

        // ──────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────

        private async Task<(string? Title, string? Body, string? TemplateKey, string? ValidationError)>
            BuildTitleBodyAsync(string mode, TemplateUseDto? template, CustomMessageDto? custom, string lang, CancellationToken ct)
        {
            mode = (mode ?? "template").ToLowerInvariant();

            if (mode == "template")
            {
                if (template == null || string.IsNullOrWhiteSpace(template.Key))
                    return (null, null, null, "template.key is required when mode=template");

                if (!HardcodedTemplateRegistry.All.TryGetValue(template.Key, out var entry))
                    return (null, null, null, $"Unknown template key: {template.Key}");

                var providedParams = template.Params ?? new Dictionary<string, string>();
                var missing = entry.RequiredParams.Where(p => !providedParams.ContainsKey(p)).ToList();
                if (missing.Count > 0)
                    return (null, null, null, $"Missing template params: {string.Join(", ", missing)}");

                var (t, b) = await _resolver.ResolveAsync(template.Key, lang, providedParams, ct);
                return (t, b, template.Key, null);
            }
            else if (mode == "custom")
            {
                if (custom == null)
                    return (null, null, null, "custom payload is required when mode=custom");

                if (string.IsNullOrWhiteSpace(custom.TitleEn) || string.IsNullOrWhiteSpace(custom.BodyEn))
                    return (null, null, null, "custom.titleEn and custom.bodyEn are required");

                var title = lang == Languages.Ar
                    ? (string.IsNullOrWhiteSpace(custom.TitleAr) ? custom.TitleEn : custom.TitleAr!)
                    : custom.TitleEn;
                var body = lang == Languages.Ar
                    ? (string.IsNullOrWhiteSpace(custom.BodyAr) ? custom.BodyEn : custom.BodyAr!)
                    : custom.BodyEn;
                return (title, body, null, null);
            }
            else
            {
                return (null, null, null, $"Unknown mode: {mode}. Expected 'template' or 'custom'");
            }
        }

        private static IEnumerable<string> ExtractPlaceholders(string? input)
        {
            if (string.IsNullOrEmpty(input)) yield break;
            foreach (Match m in Placeholder.Matches(input))
                yield return m.Groups[1].Value;
        }
    }
}
