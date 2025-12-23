using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Persistence.Entities.Main.Paymob;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/v1/admin/payment")]
    public class AdminPaymentController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public AdminPaymentController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// GET /api/v1/admin/payment/card-webhooks
        /// Get paginated list of card token webhook logs
        /// </summary>
        [HttpGet("card-webhooks")]
        public async Task<IActionResult> GetCardWebhookLogs(
            [FromQuery] CardTokenStatus? status = null,
            [FromQuery] string? userId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _serviceManager.PaymobService.GetCardWebhookLogsAsync(status, userId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/v1/admin/payment/card-webhooks/stats
        /// Get statistics for card token webhooks (counts by status, top failure reasons)
        /// </summary>
        [HttpGet("card-webhooks/stats")]
        public async Task<IActionResult> GetCardWebhookStats(CancellationToken ct = default)
        {
            var result = await _serviceManager.PaymobService.GetCardWebhookStatsAsync();
            return Ok(result);
        }

        /// <summary>
        /// GET /api/v1/admin/payment/card-webhooks/{id}
        /// Get detailed card token webhook log (includes raw payload)
        /// </summary>
        [HttpGet("card-webhooks/{id:int}")]
        public async Task<IActionResult> GetCardWebhookDetail(int id, CancellationToken ct = default)
        {
            var result = await _serviceManager.PaymobService.GetCardWebhookDetailAsync(id);
            return Ok(result);
        }
    }
}
