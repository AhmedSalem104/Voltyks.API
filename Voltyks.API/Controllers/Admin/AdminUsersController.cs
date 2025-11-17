using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminUsersController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/users?search=
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.GetUsersAsync(search, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/users/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(
            string id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.GetUserByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/users/{id}/ban-toggle
        /// </summary>
        [HttpPost("{id}/ban-toggle")]
        public async Task<IActionResult> ToggleBan(
            string id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.ToggleBanUserAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/users/{id}/wallet
        /// </summary>
        [HttpGet("{id}/wallet")]
        public async Task<IActionResult> GetUserWallet(
            string id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.GetUserWalletAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/users/{id}/vehicles
        /// </summary>
        [HttpGet("{id}/vehicles")]
        public async Task<IActionResult> GetUserVehicles(
            string id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.GetUserVehiclesAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/users/{id}/reports
        /// </summary>
        [HttpGet("{id}/reports")]
        public async Task<IActionResult> GetUserReports(
            string id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.GetUserReportsAsync(id, ct);
            return Ok(result);
        }
    }
}
