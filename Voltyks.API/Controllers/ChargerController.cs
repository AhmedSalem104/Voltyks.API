using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.Charger;

namespace Voltyks.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChargerController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public ChargerController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet("GetCapacity")]
        public async Task<IActionResult> GetCapacity()
        {
            var result = await _serviceManager.ChargerService.GetAllCapacitiesAsync();
            return Ok(result);
        }

        [HttpGet("GetProtocol")]
        public async Task<IActionResult> GetProtocol()
        {
            var result = await _serviceManager.ChargerService.GetAllProtocolsAsync();
            return Ok(result);
        }

        [HttpGet("GetPriceByCapacity")]
        public async Task<IActionResult> GetPriceBasedOnCapacity()
        {
            var result = await _serviceManager.ChargerService.GetPriceListBasedOnCapacityAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpPost("AddCharger")]
        public async Task<IActionResult> AddCharger([FromBody] AddChargerDto dto)
        {
            var result = await _serviceManager.ChargerService.AddChargerAsync(dto);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("GetChargersByUser")]
        public async Task<IActionResult> GetMyChargers()
        {
            var result = await _serviceManager.ChargerService.GetChargersForCurrentUserAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpPut("ToggleStatus")]
        public async Task<IActionResult> ToggleStatus(int chargerId)
        {
            var result = await _serviceManager.ChargerService.ToggleChargerStatusAsync(chargerId);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("UpdateCharger")]
        public async Task<IActionResult> UpdateCharger([FromBody] UpdateChargerDto dto , int chargerId)
        {
            var result = await _serviceManager.ChargerService.UpdateChargerAsync(dto , chargerId);
            return Ok(result);
        }

        [HttpDelete("DeleteCharger")]
        [Authorize]
        public async Task<IActionResult> DeleteCharger(int chargerId)
        {
            var result = await _serviceManager.ChargerService.DeleteChargerAsync(chargerId);
            return Ok(result);
        }

    }

}
