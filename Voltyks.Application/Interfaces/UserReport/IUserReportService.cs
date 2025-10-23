using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Report;
using Voltyks.Core.DTOs;

namespace Voltyks.Application.Interfaces.UserReport
{
    public interface IUserReportService
    {
        Task<ApiResponse<object>> CreateReportAsync(ReportDto dto, CancellationToken ct = default);
        Task<ApiResponse<List<ReportDto>>> GetReportsAsync(ReportFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<ReportDto>> GetReportByIdAsync(int reportId, CancellationToken ct = default);
    }

}
