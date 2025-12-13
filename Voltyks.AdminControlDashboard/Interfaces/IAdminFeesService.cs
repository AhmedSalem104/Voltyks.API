using Voltyks.AdminControlDashboard.Dtos.Fees;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminFeesService
    {
        Task<ApiResponse<AdminFeesDto>> GetFeesAsync(CancellationToken ct = default);
        Task<ApiResponse<object>> UpdateFeesAsync(UpdateFeesDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> TransferFeesAsync(TransferFeesRequestDto dto, CancellationToken ct = default);
        Task<ApiResponse<List<WalletTransactionDto>>> GetWalletTransactionsAsync(string userId, CancellationToken ct = default);
    }
}
