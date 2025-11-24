using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.Chargers;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/chargers")]
    public class AdminChargersController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminChargersController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/chargers?userId= - Get all chargers (optionally filtered by user)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetChargers(
            [FromQuery] int? userId = null,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminChargersService.GetChargersAsync(userId, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/chargers/{id} - Get charger by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChargerById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminChargersService.GetChargerByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/chargers - Create new charger
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCharger(
            [FromBody] AdminCreateChargerDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminChargersService.CreateChargerAsync(dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// PUT /api/admin/chargers/{id} - Update charger
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCharger(
            int id,
            [FromBody] AdminUpdateChargerDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminChargersService.UpdateChargerAsync(id, dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/admin/chargers/{id} - Delete charger (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCharger(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminChargersService.DeleteChargerAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// PATCH /api/admin/chargers/{id}/status - Toggle charger active status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleChargerStatus(
            int id,
            [FromQuery] bool isActive,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminChargersService.ToggleChargerStatusAsync(id, isActive, ct);
            return Ok(result);
        }
    }
}
