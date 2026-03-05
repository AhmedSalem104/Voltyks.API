using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Application.Services.Background.Backup;
using Voltyks.Core.DTOs;

namespace Voltyks.API.Controllers.Admin
{
    [ApiController]
    [Route("api/v1/cron")]
    [AllowAnonymous]
    public class CronRatingController : ControllerBase
    {
        private readonly ILogger<CronRatingController> _logger;

        public CronRatingController(ILogger<CronRatingController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// POST /api/v1/cron/process-ratings?key={CronApiKey}
        /// External cron endpoint — validates API key instead of JWT.
        /// Triggers expired rating window processing (same logic as RatingWindowService).
        /// </summary>
        [HttpPost("process-ratings")]
        public async Task<ActionResult<ApiResponse<object>>> ProcessRatings(
            [FromQuery] string key,
            [FromServices] IRatingWindowProcessor processor,
            [FromServices] IOptions<DatabaseBackupOptions> options,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(options.Value.CronApiKey) ||
                !string.Equals(key, options.Value.CronApiKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("Cron process-ratings attempt with invalid API key");
                return Unauthorized(new ApiResponse<object>(
                    message: "Invalid API key",
                    status: false
                ));
            }

            _logger.LogInformation("Cron process-ratings triggered");

            await processor.ProcessExpiredWindowsAsync(ct);

            return Ok(new ApiResponse<object>(
                message: "Rating window processing completed",
                status: true
            ));
        }
    }
}
