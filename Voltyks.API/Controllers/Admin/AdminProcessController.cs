using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/process")]
    public class AdminProcessController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminProcessController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/process - Get all processes with full related data
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProcesses(CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminProcessService.GetProcessesAsync(ct);
            return Ok(result);
        }
    }
}
