using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard;
using Voltyks.Persistence;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/process")]
    public class AdminProcessController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;
        private readonly IDbInitializer _dbInitializer;
        private readonly UserManager<AppUser> _userManager;
        private readonly VoltyksDbContext _context;

        public AdminProcessController(IAdminServiceManager adminServiceManager, IDbInitializer dbInitializer, UserManager<AppUser> userManager, VoltyksDbContext context)
        {
            _adminServiceManager = adminServiceManager;
            _dbInitializer = dbInitializer;
            _userManager = userManager;
            _context = context;
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
        public async Task<IActionResult> ForceSeed()
        {
            await _dbInitializer.ForceSeedAsync();
            return Ok(new { status = true, message = "Data seeded successfully" });
        }

        /// <summary>
        /// DELETE /api/admin/process/reset-admin - Delete admin user and reseed
        /// </summary>
        [HttpDelete("reset-admin")]
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

        /// <summary>
        /// POST /api/admin/process/seed-price-options - Seed PriceOptions with increment of 10 (100-5000)
        /// </summary>
        [HttpPost("seed-price-options")]
        public async Task<IActionResult> SeedPriceOptions()
        {
            try
            {
                // Get existing values
                var existingValues = await _context.PriceOptions.Select(p => p.Value).ToListAsync();

                // Generate values from 100 to 5000 with increment of 10
                var newPriceOptions = new List<PriceOption>();
                for (int value = 100; value <= 5000; value += 10)
                {
                    if (!existingValues.Contains(value))
                    {
                        newPriceOptions.Add(new PriceOption { Value = value });
                    }
                }

                if (newPriceOptions.Any())
                {
                    await _context.PriceOptions.AddRangeAsync(newPriceOptions);
                    await _context.SaveChangesAsync();
                }

                var totalCount = await _context.PriceOptions.CountAsync();
                return Ok(new { status = true, message = $"Added {newPriceOptions.Count} new price options. Total: {totalCount}", data = new { added = newPriceOptions.Count, total = totalCount } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }
    }
}
