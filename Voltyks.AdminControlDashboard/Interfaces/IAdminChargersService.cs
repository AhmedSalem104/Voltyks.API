using Voltyks.AdminControlDashboard.Dtos.Chargers;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminChargersService
    {
        Task<ApiResponse<List<AdminChargerDto>>> GetChargersAsync(int? userId = null, CancellationToken ct = default);
        Task<ApiResponse<AdminChargerDto>> GetChargerByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<AdminChargerDto>> CreateChargerAsync(AdminCreateChargerDto dto, CancellationToken ct = default);
        Task<ApiResponse<AdminChargerDto>> UpdateChargerAsync(int id, AdminUpdateChargerDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> DeleteChargerAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<object>> ToggleChargerStatusAsync(int id, bool isActive, CancellationToken ct = default);
    }
}
