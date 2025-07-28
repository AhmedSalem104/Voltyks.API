using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces.ChargingRequest;
using Voltyks.Application.Services.ChargingRequest;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs.ChargerRequest;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChargingRequestController : ControllerBase
    {
        private readonly IServiceManager _service;
        public ChargingRequestController(IServiceManager service)
        {
            _service = service;
        }

        [HttpPost("sendChargingRequest")]
        public async Task<IActionResult> SendChargingRequest([FromBody] SendChargingRequestDto dto)
        {
            var result = await _service.ChargingRequestService.SendChargingRequestAsync(dto);
            return Ok(result);
        }



        [HttpPost("registerDeviceToken")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] DeviceTokenDto token)
        {
            var result = await _service.ChargingRequestService.RegisterDeviceTokenAsync(token);
            return Ok(result);
        }


    }
}
