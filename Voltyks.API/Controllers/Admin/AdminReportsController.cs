using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.Reports;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/reports")]
    public class AdminReportsController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminReportsController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/reports
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetReports(
            [FromQuery] AdminReportFilterDto? filter = null,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminReportsService.GetReportsAsync(filter, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/reports/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(
            int id,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminReportsService.GetReportByIdAsync(id, ct);
            return Ok(result);
        }
    }
}
