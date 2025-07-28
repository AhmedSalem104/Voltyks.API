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

        /// Create a new charging request and notify the charger owner.     
        Task<ApiResponse<ChargerDetailsDto>> SendChargingRequestAsync(SendChargingRequestDto dto );
        Task<ApiResponse<bool>> RegisterDeviceTokenAsync(DeviceTokenDto token);


        ///// Handle when the charger owner accepts the request.   
        //Task AcceptRequestAsync(int requestId, string ownerId);


        ///// Handle when the charger owner rejects the request.
        //Task RejectRequestAsync(int requestId, string ownerId);


        ///// Confirm the request after the charger owner accepts and the car owner confirms.      
        //Task ConfirmRequestAsync(int requestId, string carOwnerId);


        ///// Get full request details (if needed for UI or audit).  
        //Task<ChargingRequestDetailsDto> GetRequestDetailsAsync(int requestId);
    }
}
