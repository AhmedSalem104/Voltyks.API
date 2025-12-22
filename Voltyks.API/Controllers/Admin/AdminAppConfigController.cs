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
        /// PATCH /api/v1/admin/app-config/mobile-enabled
        /// Update mobile app kill-switch status
        /// </summary>
        [HttpPatch("mobile-enabled")]
        public async Task<IActionResult> UpdateMobileEnabled(
            [FromBody] UpdateMobileAppConfigDto dto,
            CancellationToken ct = default)
        {
            var result = await _serviceManager.MobileAppConfigService.UpdateAsync(dto);
            return Ok(result);
        }
    }
}
