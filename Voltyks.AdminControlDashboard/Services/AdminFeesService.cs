using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voltyks.AdminControlDashboard.Dtos.Fees;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Application.Interfaces.FeesConfig;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.FeesConfig;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminFeesService : IAdminFeesService
    {
        private readonly IFeesConfigService _feesConfigService;
        private readonly VoltyksDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminFeesService(IFeesConfigService feesConfigService, VoltyksDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _feesConfigService = feesConfigService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<AdminFeesDto>> GetFeesAsync(CancellationToken ct = default)
        {
            try
            {
                // Pure wrapper - call existing service
                var result = await _feesConfigService.GetAsync();

                if (!result.Status)
                {
                    return new ApiResponse<AdminFeesDto>(result.Message, false, result.Errors);
                }

                var adminDto = new AdminFeesDto
                {
                    Id = 1, // Default ID since FeesConfig is usually single row
                    MinimumFee = result.Data.MinimumFee,
                    Percentage = result.Data.Percentage,
                    UpdatedAt = result.Data.UpdatedAt,
                    UpdatedBy = result.Data.UpdatedBy
                };

                return new ApiResponse<AdminFeesDto>(adminDto, "Fees retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminFeesDto>(
                    message: "Failed to retrieve fees",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> UpdateFeesAsync(UpdateFeesDto dto, CancellationToken ct = default)
        {

            try
            {
                // Wrapper - convert to existing DTO and call service
                var updateDto = new FeesConfigUpdateDto
                {
                    MinimumFee = dto.MinimumFee,
                    Percentage = dto.Percentage
                };

                var result = await _feesConfigService.UpdateAsync(updateDto);

                if (!result.Status)
                {
                    return new ApiResponse<object>(result.Message, false, result.Errors);
                }

                return new ApiResponse<object>(
                    data: new { minimumFee = dto.MinimumFee, percentage = dto.Percentage },
                    message: "Fees updated successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to update fees",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> TransferFeesAsync(TransferFeesRequestDto dto, CancellationToken ct = default)
        {

            try
            {
                // Validate amount - reject zero only
                if (dto.Amount == 0)
                {
                    return new ApiResponse<object>(
                        message: "Amount cannot be zero",
                        status: false);
                }

                // Validate RecipientUserId
                if (string.IsNullOrWhiteSpace(dto.RecipientUserId))
                {
                    return new ApiResponse<object>(
                        message: "RecipientUserId is required",
                        status: false);
                }

                // Find recipient user
                var recipientUser = await _context.Set<AppUser>()
                    .FirstOrDefaultAsync(u => u.Id == dto.RecipientUserId, ct);

                if (recipientUser == null)
                {
                    return new ApiResponse<object>(
                        message: "Recipient user not found",
                        status: false);
                }

                // Check if user is banned
                if (recipientUser.IsBanned)
                {
                    return new ApiResponse<object>(
                        message: "Cannot transfer fees to banned user",
                        status: false);
                }

                // Get current wallet balance
                var currentWallet = recipientUser.Wallet ?? 0;

                // For negative amounts (deduction), check sufficient balance
                if (dto.Amount < 0)
                {
                    var deductAmount = Math.Abs((double)dto.Amount);
                    if (currentWallet < deductAmount)
                    {
                        return new ApiResponse<object>(
                            message: "Insufficient wallet balance",
                            status: false,
                            errors: new List<string> { $"Current balance: {currentWallet}, Requested deduction: {deductAmount}" });
                    }
                }

                // Update user wallet (works for both add and deduct)
                var newWallet = currentWallet + (double)dto.Amount;
                recipientUser.Wallet = newWallet;

                // Get current admin ID
                var adminId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                // Log the wallet transaction
                var transaction = new WalletTransaction
                {
                    UserId = dto.RecipientUserId,
                    Amount = dto.Amount,
                    TransactionType = dto.Amount > 0 ? "Add" : "Deduct",
                    Notes = dto.Notes,
                    PreviousBalance = currentWallet,
                    NewBalance = newWallet,
                    CreatedByAdminId = adminId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WalletTransactions.Add(transaction);

                // Save changes to database
                await _context.SaveChangesAsync(ct);

                // Determine operation type for message
                var operationType = dto.Amount > 0 ? "added to" : "deducted from";
                var absoluteAmount = Math.Abs(dto.Amount);

                return new ApiResponse<object>(
                    data: new
                    {
                        recipientUserId = dto.RecipientUserId,
                        recipientName = $"{recipientUser.FirstName} {recipientUser.LastName}",
                        amount = dto.Amount,
                        absoluteAmount = absoluteAmount,
                        operationType = dto.Amount > 0 ? "Add" : "Deduct",
                        previousWallet = currentWallet,
                        newWallet = newWallet,
                        notes = dto.Notes,
                        transferredAt = DateTime.UtcNow
                    },
                    message: $"{absoluteAmount} EGP {operationType} wallet successfully",
                    status: true);
            }

            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to transfer fees",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<WalletTransactionDto>>> GetWalletTransactionsAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                // Validate userId
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return new ApiResponse<List<WalletTransactionDto>>(
                        message: "UserId is required",
                        status: false);
                }

                // Check if user exists
                var user = await _context.Set<AppUser>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId, ct);

                if (user == null)
                {
                    return new ApiResponse<List<WalletTransactionDto>>(
                        message: "User not found",
                        status: false);
                }

                // Get wallet transactions for user
                var transactions = await _context.WalletTransactions
                    .AsNoTracking()
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(50)
                    .Select(t => new WalletTransactionDto
                    {
                        Id = t.Id,
                        UserId = t.UserId,
                        UserName = $"{user.FirstName} {user.LastName}",
                        Amount = t.Amount,
                        TransactionType = t.TransactionType,
                        Notes = t.Notes,
                        PreviousBalance = t.PreviousBalance,
                        NewBalance = t.NewBalance,
                        CreatedByAdminId = t.CreatedByAdminId,
                        CreatedAt = t.CreatedAt
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<WalletTransactionDto>>(
                    data: transactions,
                    message: "Wallet transactions retrieved successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<WalletTransactionDto>>(
                    message: "Failed to retrieve wallet transactions",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}