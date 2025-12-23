using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.MobileAppConfig;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/v1/admin/app-config")]
    public class AdminAppConfigController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public AdminAppConfigController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// GET /api/v1/admin/app-config/mobile-status
        /// Get full mobile app config (admin view)
        /// </summary>
        [HttpGet("mobile-status")]
        public async Task<IActionResult> GetMobileConfig(CancellationToken ct = default)
        {
            var result = await _serviceManager.MobileAppConfigService.GetAdminConfigAsync();
            return Ok(result);
        }

        /// <summary>
        /// PATCH /api/v1/admin/app-config/mobile-status
        /// Update mobile app config (kill-switch + min versions)
        /// </summary>
        [HttpPatch("mobile-status")]
        public async Task<IActionResult> UpdateMobileConfig(
            [FromBody] UpdateMobileAppConfigDto dto,
            CancellationToken ct = default)
        {
            var result = await _serviceManager.MobileAppConfigService.UpdateAsync(dto);
            return Ok(result);
        }
    }
}
