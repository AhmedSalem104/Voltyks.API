using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/complaints")]
    public class AdminComplaintsController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminComplaintsController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/complaints - Get all complaints with user and category details
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllComplaints(
            [FromQuery] bool includeResolved = true,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintsService.GetAllComplaintsAsync(includeResolved, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/complaints/{id} - Get complaint by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComplaintById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintsService.GetComplaintByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// PATCH /api/admin/complaints/{id}/status - Update complaint resolved status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateComplaintStatus(
            int id,
            [FromQuery] bool isResolved,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintsService.UpdateComplaintStatusAsync(id, isResolved, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/complaints/{id}/time-status - Check if complaint is over 12 hours old
        /// </summary>
        [HttpGet("{id}/time-status")]
        public async Task<IActionResult> GetComplaintTimeStatus(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintsService.GetComplaintTimeStatusAsync(id, ct);
            return Ok(result);
        }
    }
}
