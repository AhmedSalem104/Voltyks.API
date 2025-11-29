using Voltyks.AdminControlDashboard.Dtos.Complaints;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces.Complaints
{
    public interface IAdminComplaintsService
    {
        Task<ApiResponse<List<AdminComplaintDto>>> GetAllComplaintsAsync(bool includeResolved = true, CancellationToken ct = default);
        Task<ApiResponse<AdminComplaintDto>> GetComplaintByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<object>> UpdateComplaintStatusAsync(int id, bool isResolved, CancellationToken ct = default);
    }
}
