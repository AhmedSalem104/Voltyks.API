using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Voltyks.Application.Interfaces.AppSettings;
using Voltyks.Core.DTOs.AppSettings;

namespace Voltyks.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/settings")]
    [Authorize(Roles = "Admin")]
    public class AdminAppSettingsController : ControllerBase
    {
        private readonly IAppSettingsService _appSettingsService;

        public AdminAppSettingsController(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
        }

        [HttpGet("charging-mode")]
        public async Task<IActionResult> GetChargingMode(CancellationToken ct)
        {
            var result = await _appSettingsService.GetChargingModeStatusAsync(ct);
            return Ok(result);
        }

        [HttpPatch("charging-mode")]
        public async Task<IActionResult> SetChargingMode([FromBody] SetChargingModeDto dto, CancellationToken ct)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var result = await _appSettingsService.SetChargingModeAsync(dto.Enabled, adminId, ct);
            return Ok(result);
        }

        [HttpPost("activate-all-chargers")]
        public async Task<IActionResult> ActivateAllChargers(CancellationToken ct)
        {
            var result = await _appSettingsService.ActivateAllInactiveChargersAsync(ct);
            return Ok(result);
        }
    }
}
