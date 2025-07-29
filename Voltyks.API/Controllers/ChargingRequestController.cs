using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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


        [HttpPost("registerDeviceToken")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] DeviceTokenDto token)
        {
            var result = await _service.ChargingRequestService.RegisterDeviceTokenAsync(token);
            return Ok(result);
        }


        [HttpPost("sendChargingRequest")]
        [Authorize]
        public async Task<IActionResult> SendChargingRequest([FromBody] SendChargingRequestDto dto)
        {
            var result = await _service.ChargingRequestService.SendChargingRequestAsync(dto);
            return Ok(result);
        }

        [HttpPut("accept/{requestId}")]
        [Authorize]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var result = await _service.ChargingRequestService.AcceptRequestAsync(requestId);
            return Ok(result);
        }


        [HttpPut("reject/{requestId}")]
        [Authorize]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var result = await _service.ChargingRequestService.RejectRequestAsync(requestId);
            return Ok(result);
        }


        [HttpPut("confirm/{requestId}")]
        [Authorize]
        public async Task<IActionResult> ConfirmRequest(int requestId)
        {
            var result = await _service.ChargingRequestService.ConfirmRequestAsync(requestId);
            return Ok(result);
        }


        [HttpGet("details/{requestId}")]
        [Authorize]
        public async Task<IActionResult> GetRequestDetails(int requestId)
        {
            var result = await _service.ChargingRequestService.GetRequestDetailsAsync(requestId);
            return Ok(result);
        }


    }
}
