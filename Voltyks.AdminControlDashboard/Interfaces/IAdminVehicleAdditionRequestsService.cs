using Voltyks.AdminControlDashboard.Dtos.VehicleAdditionRequests;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminVehicleAdditionRequestsService
    {
        Task<ApiResponse<PagedResult<AdminVehicleAdditionRequestDto>>> GetAllAsync(
            string? status, PaginationParams? pagination, CancellationToken ct = default);

        Task<ApiResponse<AdminVehicleAdditionRequestDto>> GetByIdAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<AcceptPreviewDto>> GetAcceptPreviewAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<object>> AcceptAsync(int id, string adminId, AcceptVehicleAdditionRequestDto? overrides, CancellationToken ct = default);

        Task<ApiResponse<object>> DeclineAsync(int id, string adminId, DeclineVehicleAdditionRequestDto? body, CancellationToken ct = default);
    }
}
