using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces.AppSettings;
using Voltyks.Application.ServicesManager.ServicesManager;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/v1/app-config")]
    public class AppConfigController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        private readonly IAppSettingsService _appSettingsService;

        public AppConfigController(IServiceManager serviceManager, IAppSettingsService appSettingsService)
        {
            _serviceManager = serviceManager;
            _appSettingsService = appSettingsService;
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
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetMobileStatus(
            [FromQuery] string? platform = null,
            [FromQuery] string? version = null,
            CancellationToken ct = default)
        {
            var result = await _serviceManager.MobileAppConfigService.GetStatusAsync(platform, version);

            // Serialize with explicit Content-Length to prevent iOS -1103 error
            // (chunked transfer encoding without Content-Length confuses NSURLSession)
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return Content(json, "application/json; charset=utf-8");
        }

        /// <summary>
        /// GET /api/v1/app-config/charging-mode-status
        /// Public endpoint for users to check if charging mode is enabled
        /// </summary>
        [HttpGet("charging-mode-status")]
        public async Task<IActionResult> GetChargingModeStatus(CancellationToken ct = default)
        {
            var result = await _appSettingsService.GetChargingModeStatusAsync(ct);
            return Ok(result);
        }
    }
}
