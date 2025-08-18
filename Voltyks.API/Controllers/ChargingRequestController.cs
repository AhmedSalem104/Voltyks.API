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
        [Authorize]
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

        [HttpPut("AcceptRequest")]
        [Authorize]
        public async Task<IActionResult> AcceptRequest([FromBody] TransRequest dto)
        {
            var result = await _service.ChargingRequestService.AcceptRequestAsync(dto);
            return Ok(result);
        }


        [HttpPut("RejectRequest")]
        [Authorize]
        public async Task<IActionResult> RejectRequest([FromBody] TransRequest dto)
        {
            var result = await _service.ChargingRequestService.RejectRequestAsync(dto);
            return Ok(result);
        }


        [HttpPut("ConfirmRequest")]
        [Authorize]
        public async Task<IActionResult> ConfirmRequest([FromBody] TransRequest dto)
        {
            var result = await _service.ChargingRequestService.ConfirmRequestAsync(dto);
            return Ok(result);
        }
        [HttpPut("abortRequest")]
        [Authorize]
        public async Task<IActionResult> AbortRequest([FromBody] TransRequest dto)
        {
            var result = await _service.ChargingRequestService.AbortRequestAsync(dto);
            return Ok(result);
        }


        [HttpPost("GetRequestDetailsById")]
        [Authorize]
        public async Task<IActionResult> GetRequestDetails([FromBody] RequestDetailsDto dto)
        {
            var result = await _service.ChargingRequestService.GetRequestDetailsAsync(dto);
            return Ok(result);
        }


    }
}
