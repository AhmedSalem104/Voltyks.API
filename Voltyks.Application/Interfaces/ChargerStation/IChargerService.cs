using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs;

namespace Voltyks.Application.Interfaces.ChargerStation
{
    public interface IChargerService
    {
        Task<ApiResponse<IEnumerable<CapacityDto>>> GetAllCapacitiesAsync();
        Task<ApiResponse<IEnumerable<ProtocolDto>>> GetAllProtocolsAsync();
        Task<ApiResponse<IEnumerable<PriceByCapacityDto>>> GetPriceListBasedOnCapacityAsync();
        Task<ApiResponse<string>> AddChargerAsync(AddChargerDto dto);
        Task<ApiResponse<IEnumerable<ChargerDto>>> GetChargersForCurrentUserAsync();
        Task<ApiResponse<string>> ToggleChargerStatusAsync(int chargerId);
        Task<ApiResponse<string>> UpdateChargerAsync(UpdateChargerDto dto);
        Task<ApiResponse<string>> DeleteChargerAsync(int chargerId);

    }

}
