using Voltyks.AdminControlDashboard.Dtos.ComplaintCategories;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminComplaintCategoriesService
    {
        Task<ApiResponse<List<AdminComplaintCategoryDto>>> GetCategoriesAsync(bool includeDeleted = false, CancellationToken ct = default);
        Task<ApiResponse<AdminComplaintCategoryDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<AdminComplaintCategoryDto>> CreateCategoryAsync(CreateComplaintCategoryDto dto, CancellationToken ct = default);
        Task<ApiResponse<AdminComplaintCategoryDto>> UpdateCategoryAsync(int id, UpdateComplaintCategoryDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> DeleteCategoryAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<object>> RestoreCategoryAsync(int id, CancellationToken ct = default);
    }
}
