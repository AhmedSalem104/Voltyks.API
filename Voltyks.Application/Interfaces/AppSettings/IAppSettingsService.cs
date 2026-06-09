using Voltyks.Core.DTOs;

namespace Voltyks.Application.Interfaces.AppSettings
{
    public interface IAppSettingsService
    {
        Task<bool> IsChargingModeEnabledAsync(CancellationToken ct = default);
        Task<ApiResponse<object>> GetChargingModeStatusAsync(CancellationToken ct = default);
        Task<ApiResponse<bool>> SetChargingModeAsync(bool enabled, string adminId, CancellationToken ct = default);
        Task<ApiResponse<int>> ActivateAllInactiveChargersAsync(CancellationToken ct = default);
        Task<bool> IsAdminsModeActivatedAsync(CancellationToken ct = default);
        Task<ApiResponse<object>> GetAdminsModeStatusAsync(CancellationToken ct = default);
        Task<ApiResponse<bool>> SetAdminsModeAsync(bool activated, string adminId, CancellationToken ct = default);

        Task<bool> IsAntiOtpRestrictionModeAsync(CancellationToken ct = default);
        Task<ApiResponse<object>> GetAntiOtpRestrictionModeStatusAsync(CancellationToken ct = default);
        Task<ApiResponse<bool>> SetAntiOtpRestrictionModeAsync(bool enabled, string adminId, CancellationToken ct = default);

        Task<bool> IsAntiPaymentRestrictionModeAsync(CancellationToken ct = default);
        Task<ApiResponse<object>> GetAntiPaymentRestrictionModeStatusAsync(CancellationToken ct = default);
        Task<ApiResponse<bool>> SetAntiPaymentRestrictionModeAsync(bool enabled, string adminId, CancellationToken ct = default);
    }
}
