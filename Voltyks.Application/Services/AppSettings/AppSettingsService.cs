using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.AppSettings;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services.AppSettings
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly VoltyksDbContext _context;

        public AppSettingsService(VoltyksDbContext context)
        {
            _context = context;
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

        public async Task<bool> IsChargingModeEnabledAsync(CancellationToken ct = default)
        {
            var settings = await GetOrCreateSettingsAsync(ct);
            return settings.ChargingModeEnabled;
        }

        public async Task<ApiResponse<object>> GetChargingModeStatusAsync(CancellationToken ct = default)
        {
            var settings = await GetOrCreateSettingsAsync(ct);

            var data = new
            {
                chargingModeEnabled = settings.ChargingModeEnabled,
                enabledAt = settings.ChargingModeEnabledAt,
                updatedBy = settings.UpdatedBy,
                updatedAt = settings.UpdatedAt,
                message = settings.ChargingModeEnabled
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

            var message = enabled
                ? "Charging mode enabled successfully"
                : "Charging mode disabled successfully";

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
