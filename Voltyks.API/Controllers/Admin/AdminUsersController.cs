using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
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
        /// GET /api/admin/users?search=&includeDeleted=false
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] bool includeDeleted = false,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.GetUsersAsync(search, includeDeleted, ct);
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

        /// <summary>
        /// DELETE /api/admin/users/{id} - Soft delete user (marks as deleted but keeps in DB)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteUser(
            string id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.SoftDeleteUserAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/admin/users/{id}/permanent - Hard delete user (permanently removes from DB)
        /// </summary>
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> HardDeleteUser(
            string id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.HardDeleteUserAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// PATCH /api/admin/users/{id}/restore - Restore a soft-deleted user
        /// </summary>
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreUser(
            string id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminUsersService.RestoreUserAsync(id, ct);
            return Ok(result);
        }
    }
}
