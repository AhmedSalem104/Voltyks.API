using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/v1/app-config")]
    public class AppConfigController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public AppConfigController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// GET /api/v1/app-config/mobile-enabled
        /// Legacy endpoint - backwards compatible
        /// Returns true if BOTH platforms are enabled
        /// </summary>
        [HttpGet("mobile-enabled")]
        public async Task<IActionResult> GetMobileEnabled(CancellationToken ct = default)
        {
            var result = await _serviceManager.MobileAppConfigService.GetLegacyStatusAsync();
            return Ok(result);
        }

        /// <summary>
        /// GET /api/v1/app-config/mobile-status
        /// New endpoint with platform and version support
        /// </summary>
        /// <param name="platform">Platform: "android" or "ios"</param>
        /// <param name="version">App version: "1.2.0"</param>
        [HttpGet("mobile-status")]
        public async Task<IActionResult> GetMobileStatus(
            [FromQuery] string? platform = null,
            [FromQuery] string? version = null,
            CancellationToken ct = default)
        {
            var result = await _serviceManager.MobileAppConfigService.GetStatusAsync(platform, version);
            return Ok(result);
        }
    }
}
