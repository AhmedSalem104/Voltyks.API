using Voltyks.AdminControlDashboard.Dtos.Vehicles;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminVehiclesService
    {
        Task<ApiResponse<List<AdminVehicleDto>>> GetVehiclesAsync(string? userId = null, CancellationToken ct = default);
        Task<ApiResponse<AdminVehicleDto>> GetVehicleByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<AdminVehicleDto>> CreateVehicleAsync(CreateVehicleDto dto, CancellationToken ct = default);
        Task<ApiResponse<AdminVehicleDto>> UpdateVehicleAsync(int id, UpdateVehicleDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> DeleteVehicleAsync(int id, CancellationToken ct = default);
    }
}
