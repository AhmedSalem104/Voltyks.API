using Voltyks.AdminControlDashboard.Dtos.Brands;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminBrandsService
    {
        // Brand CRUD
        Task<ApiResponse<List<AdminBrandDto>>> GetBrandsAsync(CancellationToken ct = default);
        Task<ApiResponse<AdminBrandDto>> GetBrandByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<AdminBrandDto>> CreateBrandAsync(CreateBrandDto dto, CancellationToken ct = default);
        Task<ApiResponse<AdminBrandDto>> UpdateBrandAsync(int id, UpdateBrandDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> DeleteBrandAsync(int id, CancellationToken ct = default);

        // Model CRUD
        Task<ApiResponse<List<AdminModelDto>>> GetModelsAsync(int? brandId = null, CancellationToken ct = default);
        Task<ApiResponse<AdminModelDto>> GetModelByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<AdminModelDto>> CreateModelAsync(CreateModelDto dto, CancellationToken ct = default);
        Task<ApiResponse<AdminModelDto>> UpdateModelAsync(int id, UpdateModelDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> DeleteModelAsync(int id, CancellationToken ct = default);
    }
}
