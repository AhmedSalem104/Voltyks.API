using Voltyks.AdminControlDashboard.Dtos.Capacity;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminCapacityService
    {
        Task<ApiResponse<IEnumerable<AdminCapacityDto>>> GetAllAsync(CancellationToken ct = default);
        Task<ApiResponse<AdminCapacityDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<AdminCapacityDto>> CreateAsync(CreateCapacityDto dto, CancellationToken ct = default);
        Task<ApiResponse<AdminCapacityDto>> UpdateAsync(int id, UpdateCapacityDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
