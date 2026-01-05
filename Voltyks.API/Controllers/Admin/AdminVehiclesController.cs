using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.Vehicles;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/vehicles")]
    public class AdminVehiclesController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminVehiclesController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/vehicles?userId= - Get all vehicles (optionally filtered by user)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVehicles(
            [FromQuery] string? userId = null,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminVehiclesService.GetVehiclesAsync(userId, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/vehicles/{id} - Get vehicle by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicleById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminVehiclesService.GetVehicleByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/vehicles - Create new vehicle
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateVehicle(
            [FromBody] AdminCreateVehicleDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminVehiclesService.CreateVehicleAsync(dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// PUT /api/admin/vehicles/{id} - Update vehicle
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicle(
            int id,
            [FromBody] AdminUpdateVehicleDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminVehiclesService.UpdateVehicleAsync(id, dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/admin/vehicles/{id} - Delete vehicle (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminVehiclesService.DeleteVehicleAsync(id, ct);
            return Ok(result);
        }
    }
}
