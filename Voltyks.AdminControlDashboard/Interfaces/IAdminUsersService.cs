using Voltyks.AdminControlDashboard.Dtos.Users;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminUsersService
    {
        Task<ApiResponse<List<AdminUserDto>>> GetUsersAsync(string? search = null, CancellationToken ct = default);
        Task<ApiResponse<AdminUserDetailsDto>> GetUserByIdAsync(string userId, CancellationToken ct = default);
        Task<ApiResponse<object>> ToggleBanUserAsync(string userId, CancellationToken ct = default);
        Task<ApiResponse<AdminWalletDto>> GetUserWalletAsync(string userId, CancellationToken ct = default);
        Task<ApiResponse<List<AdminUserVehicleDto>>> GetUserVehiclesAsync(string userId, CancellationToken ct = default);
        Task<ApiResponse<List<AdminUserReportDto>>> GetUserReportsAsync(string userId, CancellationToken ct = default);
    }
}
