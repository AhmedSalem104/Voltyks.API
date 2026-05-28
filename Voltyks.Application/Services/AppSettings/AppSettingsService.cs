using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.AppSettings;
using Voltyks.Application.Interfaces.Caching;
using Voltyks.Application.Services.Caching;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services.AppSettings
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly VoltyksDbContext _context;
        private readonly ICacheService _cacheService;

        public AppSettingsService(VoltyksDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        private async Task<Persistence.Entities.Main.AppSettings> GetOrCreateSettingsAsync(CancellationToken ct = default)
        {
            var settings = await _context.AppSettings.FindAsync(new object[] { 1 }, ct);
            if (settings == null)
            {
                settings = new Persistence.Entities.Main.AppSettings
                {
                    Id = 1,
                    ChargingModeEnabled = false,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.AppSettings.AddAsync(settings, ct);
                await _context.SaveChangesAsync(ct);
            }
            return settings;
        }

        private async Task<AppSettingsSnapshot> GetSnapshotAsync(CancellationToken ct = default)
        {
            AppSettingsSnapshot? cached = null;
            try { cached = await _cacheService.GetAsync<AppSettingsSnapshot>(CacheKeys.AppSettings); }
            catch { /* cache backend unavailable — fall through to DB */ }

            if (cached is not null)
                return cached;

            var settings = await GetOrCreateSettingsAsync(ct);
            var snapshot = new AppSettingsSnapshot
            {
                ChargingModeEnabled = settings.ChargingModeEnabled,
                AdminsModeActivated = settings.AdminsModeActivated,
                ChargingModeEnabledAt = settings.ChargingModeEnabledAt,
                UpdatedBy = settings.UpdatedBy,
                UpdatedAt = settings.UpdatedAt
            };

            try { await _cacheService.SetAsync(CacheKeys.AppSettings, snapshot, CacheKeys.Duration.FifteenMinutes); }
            catch { /* cache backend unavailable — proceed with DB result */ }

            return snapshot;
        }

        private async Task InvalidateSnapshotAsync()
        {
            try { await _cacheService.RemoveAsync(CacheKeys.AppSettings); }
            catch { /* cache backend unavailable — staleness bounded by TTL */ }
        }

        public async Task<bool> IsChargingModeEnabledAsync(CancellationToken ct = default)
        {
            var snapshot = await GetSnapshotAsync(ct);
            return snapshot.ChargingModeEnabled;
        }

        public async Task<ApiResponse<object>> GetChargingModeStatusAsync(CancellationToken ct = default)
        {
            var snapshot = await GetSnapshotAsync(ct);

            var data = new
            {
                chargingModeEnabled = snapshot.ChargingModeEnabled,
                enabledAt = snapshot.ChargingModeEnabledAt,
                updatedBy = snapshot.UpdatedBy,
                updatedAt = snapshot.UpdatedAt,
                message = snapshot.ChargingModeEnabled
                    ? "Charging is fully operational"
                    : "Charging is in setup mode. New chargers will be activated by admin."
            };

            return new ApiResponse<object>(data, "Success", true);
        }

        public async Task<ApiResponse<bool>> SetChargingModeAsync(bool enabled, string adminId, CancellationToken ct = default)
        {
            var settings = await GetOrCreateSettingsAsync(ct);

            settings.ChargingModeEnabled = enabled;
            settings.UpdatedBy = adminId;
            settings.UpdatedAt = DateTime.UtcNow;

            if (enabled && !settings.ChargingModeEnabledAt.HasValue)
            {
                settings.ChargingModeEnabledAt = DateTime.UtcNow;
            }

            _context.AppSettings.Update(settings);
            await _context.SaveChangesAsync(ct);
            await InvalidateSnapshotAsync();

            var message = enabled
                ? "Charging mode enabled successfully"
                : "Charging mode disabled successfully";

            return new ApiResponse<bool>(true, message, true);
        }

        public async Task<bool> IsAdminsModeActivatedAsync(CancellationToken ct = default)
        {
            var snapshot = await GetSnapshotAsync(ct);
            return snapshot.AdminsModeActivated;
        }

        public async Task<ApiResponse<object>> GetAdminsModeStatusAsync(CancellationToken ct = default)
        {
            var snapshot = await GetSnapshotAsync(ct);

            var data = new
            {
                adminsModeActivated = snapshot.AdminsModeActivated,
                registrationEnabled = !snapshot.AdminsModeActivated,
                message = snapshot.AdminsModeActivated
                    ? "System is in admin-only mode. New registrations are disabled."
                    : "Registration is open for new users."
            };

            return new ApiResponse<object>(data, "Success", true);
        }

        public async Task<ApiResponse<bool>> SetAdminsModeAsync(bool activated, string adminId, CancellationToken ct = default)
        {
            var settings = await GetOrCreateSettingsAsync(ct);

            settings.AdminsModeActivated = activated;
            settings.UpdatedBy = adminId;
            settings.UpdatedAt = DateTime.UtcNow;

            _context.AppSettings.Update(settings);
            await _context.SaveChangesAsync(ct);
            await InvalidateSnapshotAsync();

            var message = activated
                ? "Admins mode activated — new registrations are now disabled"
                : "Admins mode deactivated — registration is now open";

            return new ApiResponse<bool>(true, message, true);
        }

        public async Task<ApiResponse<int>> ActivateAllInactiveChargersAsync(CancellationToken ct = default)
        {
            var inactiveChargers = await _context.Chargers
                .Where(c => !c.IsActive && !c.IsDeleted)
                .ToListAsync(ct);

            foreach (var charger in inactiveChargers)
            {
                charger.IsActive = true;
            }

            await _context.SaveChangesAsync(ct);

            return new ApiResponse<int>(
                inactiveChargers.Count,
                $"{inactiveChargers.Count} chargers activated successfully",
                true);
        }
    }
}
