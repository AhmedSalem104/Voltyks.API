using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.Persistence;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/process")]
    public class AdminProcessController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;
        private readonly IDbInitializer _dbInitializer;
        private readonly UserManager<AppUser> _userManager;

        public AdminProcessController(IAdminServiceManager adminServiceManager, IDbInitializer dbInitializer, UserManager<AppUser> userManager)
        {
            _adminServiceManager = adminServiceManager;
            _dbInitializer = dbInitializer;
            _userManager = userManager;
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

        /// <summary>
        /// DELETE /api/admin/process/reset-admin - Delete admin user and reseed
        /// </summary>
        [HttpDelete("reset-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetAdmin()
        {
            // Delete Admin user
            var adminUser = await _userManager.FindByNameAsync("Admin");
            if (adminUser != null)
            {
                await _userManager.DeleteAsync(adminUser);
            }

            // Delete Operator user
            var operatorUser = await _userManager.FindByNameAsync("operator");
            if (operatorUser != null)
            {
                await _userManager.DeleteAsync(operatorUser);
            }

            // Reseed identity
            await _dbInitializer.InitializeIdentityAsync();

            return Ok(new { status = true, message = "Admin users deleted and reseeded successfully" });
        }
    }
}
