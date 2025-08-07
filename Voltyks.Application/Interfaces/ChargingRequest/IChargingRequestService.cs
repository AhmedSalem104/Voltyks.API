using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs.ChargerRequest;

namespace Voltyks.Application.Interfaces.ChargingRequest
{
    public interface IChargingRequestService
    {
        Task<ApiResponse<ChargerDetailsDto>> SendChargingRequestAsync(SendChargingRequestDto dto);
        Task<ApiResponse<bool>> RegisterDeviceTokenAsync(DeviceTokenDto token);
        Task<ApiResponse<bool>> AcceptRequestAsync(TransRequest dto);
        Task<ApiResponse<bool>> RejectRequestAsync(TransRequest dto);
        Task<ApiResponse<bool>> ConfirmRequestAsync(TransRequest dto);
        Task<ApiResponse<ChargingRequestDetailsDto>> GetRequestDetailsAsync(RequestDetailsDto dto); 
    }


}
