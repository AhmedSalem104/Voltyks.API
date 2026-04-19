using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.VehicleAdditionRequests;

namespace Voltyks.Application.Interfaces.VehicleAdditionRequest
{
    public interface IVehicleAdditionRequestService
    {
        Task<ApiResponse<UserVehicleAdditionRequestDto>> CreateAsync(string userId, CreateVehicleAdditionRequestDto dto, CancellationToken ct = default);
        Task<ApiResponse<List<UserVehicleAdditionRequestDto>>> GetMyRequestsAsync(string userId, CancellationToken ct = default);
    }
}
