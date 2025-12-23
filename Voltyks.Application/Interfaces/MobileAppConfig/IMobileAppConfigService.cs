using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.MobileAppConfig;

namespace Voltyks.Application.Interfaces.MobileAppConfig
{
    public interface IMobileAppConfigService
    {
        /// <summary>
        /// Get mobile status for a specific platform with version check
        /// </summary>
        Task<ApiResponse<MobileAppStatusDto>> GetStatusAsync(string? platform, string? version);

        /// <summary>
        /// Get legacy status (backwards compatible - returns true if both platforms enabled)
        /// </summary>
        Task<ApiResponse<MobileAppEnabledDto>> GetLegacyStatusAsync();

        /// <summary>
        /// Get full config for admin
        /// </summary>
        Task<ApiResponse<MobileAppConfigAdminDto>> GetAdminConfigAsync();

        /// <summary>
        /// Update config (admin only)
        /// </summary>
        Task<ApiResponse<MobileAppConfigAdminDto>> UpdateAsync(UpdateMobileAppConfigDto dto);
    }
}
