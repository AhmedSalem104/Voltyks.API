using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.VehicleAdditionRequests;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/vehicle-addition-requests")]
    public class AdminVehicleAdditionRequestsController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminVehicleAdditionRequestsController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/vehicle-addition-requests
        /// List all vehicle addition requests (paginated + optional status filter).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
            var result = await _adminServiceManager.AdminVehicleAdditionRequestsService
                .GetAllAsync(status, pagination, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/vehicle-addition-requests/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminVehicleAdditionRequestsService
                .GetByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/vehicle-addition-requests/{id}/accept-preview
        /// Returns smart suggestions (similar brands/models + parsed capacity)
        /// so the admin can verify and clean the data before accepting.
        /// </summary>
        [HttpGet("{id}/accept-preview")]
        public async Task<IActionResult> GetAcceptPreview(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminVehicleAdditionRequestsService
                .GetAcceptPreviewAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/vehicle-addition-requests/{id}/accept
        /// Accept request: adds Brand (if new) + Model, notifies user.
        /// Body is optional; if provided the admin can override brand/model/capacity
        /// or link to an existing brand via UseExistingBrandId.
        /// </summary>
        [HttpPost("{id}/accept")]
        public async Task<IActionResult> Accept(
            int id,
            [FromBody] AcceptVehicleAdditionRequestDto? body = null,
            CancellationToken ct = default)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var result = await _adminServiceManager.AdminVehicleAdditionRequestsService
                .AcceptAsync(id, adminId, body, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/vehicle-addition-requests/{id}/decline
        /// Decline request and notify user (reason: vehicle already exists).
        /// </summary>
        [HttpPost("{id}/decline")]
        public async Task<IActionResult> Decline(int id, CancellationToken ct = default)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var result = await _adminServiceManager.AdminVehicleAdditionRequestsService
                .DeclineAsync(id, adminId, ct);
            return Ok(result);
        }
    }
}
