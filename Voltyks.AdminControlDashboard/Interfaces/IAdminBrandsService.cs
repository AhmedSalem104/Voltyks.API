using Voltyks.AdminControlDashboard.Dtos.Brands;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminBrandsService
    {
        Task<ApiResponse<List<AdminBrandDto>>> GetBrandsAsync(CancellationToken ct = default);
        Task<ApiResponse<List<AdminModelDto>>> GetModelsAsync(int? brandId = null, CancellationToken ct = default);
    }
}
