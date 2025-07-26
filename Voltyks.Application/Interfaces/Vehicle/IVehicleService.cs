using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.VehicleDTOs;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Interfaces
{
    public interface IVehicleService
    {
        Task<ApiResponse<VehicleDto>> CreateVehicleAsync(CreateAndUpdateVehicleDto dto);
        Task<ApiResponse<VehicleDto>> UpdateVehicleAsync(int id, CreateAndUpdateVehicleDto dto);
        Task<ApiResponse<IEnumerable<VehicleDto>>> GetVehiclesByUserIdAsync();
        Task<ApiResponse<bool>> DeleteVehicleAsync(int vehicleId);
    }
}
