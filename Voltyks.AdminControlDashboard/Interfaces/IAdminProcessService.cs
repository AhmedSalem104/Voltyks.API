using Voltyks.AdminControlDashboard.Dtos.Process;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminProcessService
    {
        Task<ApiResponse<List<AdminProcessDto>>> GetProcessesAsync(CancellationToken ct = default);
    }
}
