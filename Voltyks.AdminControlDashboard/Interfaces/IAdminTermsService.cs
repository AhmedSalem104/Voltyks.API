using Voltyks.AdminControlDashboard.Dtos.Terms;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminTermsService
    {
        Task<ApiResponse<AdminTermsDto>> GetTermsAsync(string lang = "en", CancellationToken ct = default);
        Task<ApiResponse<object>> UpdateTermsAsync(UpdateTermsDto dto, CancellationToken ct = default);
    }
}
