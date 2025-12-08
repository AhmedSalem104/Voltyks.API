using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.Persistence;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/process")]
    public class AdminProcessController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;
        private readonly IDbInitializer _dbInitializer;

        public AdminProcessController(IAdminServiceManager adminServiceManager, IDbInitializer dbInitializer)
        {
            _adminServiceManager = adminServiceManager;
            _dbInitializer = dbInitializer;
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

        /// <summary>
        /// POST /api/admin/process/force-seed - Force seed data into database
        /// </summary>
        [HttpPost("force-seed")]
        [AllowAnonymous]
        public async Task<IActionResult> ForceSeed()
        {
            await _dbInitializer.ForceSeedAsync();
            return Ok(new { status = true, message = "Data seeded successfully" });
        }
    }
}
