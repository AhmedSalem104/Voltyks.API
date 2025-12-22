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
        /// Public endpoint - no authentication required
        /// </summary>
        [HttpGet("mobile-enabled")]
        public async Task<IActionResult> GetMobileEnabled(CancellationToken ct = default)
        {
            var result = await _serviceManager.MobileAppConfigService.GetStatusAsync();
            return Ok(result);
        }
    }
}
