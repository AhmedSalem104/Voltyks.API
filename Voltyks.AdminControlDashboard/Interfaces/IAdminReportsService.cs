using Voltyks.AdminControlDashboard.Dtos.Reports;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminReportsService
    {
        Task<ApiResponse<List<AdminReportDto>>> GetReportsAsync(AdminReportFilterDto? filter = null, CancellationToken ct = default);
        Task<ApiResponse<AdminReportDetailsDto>> GetReportByIdAsync(int reportId, CancellationToken ct = default);
        Task<ApiResponse<object>> UpdateReportStatusAsync(int reportId, bool isResolved, CancellationToken ct = default);
    }
}
