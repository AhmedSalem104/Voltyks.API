using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.VehicleDTOs;

namespace Voltyks.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController(IServiceManager _serviceManager) : ControllerBase
    {
        [HttpPost("CreateVehicle")]
        public async Task<IActionResult> CreateVehicle([FromBody] CreateAndUpdateVehicleDto dto)
        {
            var result = await _serviceManager.VehicleService.CreateVehicleAsync(dto);
            return Ok(result);
        }

        [HttpPut("UpdateVehicle")]
        public async Task<IActionResult> UpdateVehicle(int id, [FromBody] CreateAndUpdateVehicleDto dto)
        {
            var result = await _serviceManager.VehicleService.UpdateVehicleAsync(id, dto);
            return Ok(result);
        }

        [HttpGet("GetVehiclesByUser")]
        public async Task<IActionResult> GetVehiclesByUserId()
        {
            var result = await _serviceManager.VehicleService.GetVehiclesByUserIdAsync();
            return Ok(result);
        }

        [HttpDelete("DeleteVehicle")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var result = await _serviceManager.VehicleService.DeleteVehicleAsync(id);
            return Ok(result);
        }
    }
    
}
