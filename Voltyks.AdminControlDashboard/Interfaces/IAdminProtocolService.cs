using Voltyks.AdminControlDashboard.Dtos.Protocol;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminProtocolService
    {
        Task<ApiResponse<AdminProtocolDto>> GetProtocolAsync(CancellationToken ct = default);
    }
}
