using Voltyks.Core.DTOs;

namespace Voltyks.Application.Interfaces.AppSettings
{
    public interface IAppSettingsService
    {
        Task<bool> IsChargingModeEnabledAsync(CancellationToken ct = default);
        Task<ApiResponse<object>> GetChargingModeStatusAsync(CancellationToken ct = default);
        Task<ApiResponse<bool>> SetChargingModeAsync(bool enabled, string adminId, CancellationToken ct = default);
        Task<ApiResponse<int>> ActivateAllInactiveChargersAsync(CancellationToken ct = default);
    }
}
