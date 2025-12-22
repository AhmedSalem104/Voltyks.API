using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.MobileAppConfig;

namespace Voltyks.Application.Interfaces.MobileAppConfig
{
    public interface IMobileAppConfigService
    {
        Task<ApiResponse<MobileAppStatusDto>> GetStatusAsync();
        Task<ApiResponse<MobileAppStatusDto>> UpdateAsync(UpdateMobileAppConfigDto dto);
    }
}
