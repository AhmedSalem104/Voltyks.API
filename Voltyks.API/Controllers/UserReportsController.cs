using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.Report;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserReportsController : ControllerBase
    {
        private readonly IServiceManager _serviceManager ;

        public UserReportsController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        // Endpoint لإنشاء تقرير جديد
        [HttpPost("create")]
        public async Task<IActionResult> CreateReport([FromBody] ReportDto dto, CancellationToken ct = default)
        {
            var result = await _serviceManager.UserReportService.CreateReportAsync(dto, ct);
            return Ok(result);
        }

        // Endpoint للحصول على جميع التقارير مع الفلترة
        [HttpGet]
        public async Task<IActionResult> GetReports([FromQuery] ReportFilterDto filter, CancellationToken ct = default)
        {
            var result = await _serviceManager.UserReportService.GetReportsAsync(filter, ct);
            return Ok(result);
        }

        // Endpoint للحصول على تقرير معين حسب الـ ReportId
        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetReportById(int reportId, CancellationToken ct = default)
        {
            var result = await _serviceManager.UserReportService.GetReportByIdAsync(reportId, ct);
            return Ok(result);
        }
    }

}
