using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.Fees;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/fees")]
    public class AdminFeesController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminFeesController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/fees
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFees(CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminFeesService.GetFeesAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// PUT /api/admin/fees
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateFees(
            [FromBody] UpdateFeesDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminFeesService.UpdateFeesAsync(dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/fees/transfer
        /// </summary>
        [HttpPost("transfer")]
        public async Task<IActionResult> TransferFees(
            [FromBody] TransferFeesRequestDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminFeesService.TransferFeesAsync(dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/fees/wallet-transactions/{userId}
        /// </summary>
        [HttpGet("wallet-transactions/{userId}")]
        public async Task<IActionResult> GetWalletTransactions(
            string userId,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminFeesService.GetWalletTransactionsAsync(userId, ct);
            return Ok(result);
        }
    }
}
