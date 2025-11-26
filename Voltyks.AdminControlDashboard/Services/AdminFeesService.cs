using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Fees;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Application.Interfaces.FeesConfig;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.FeesConfig;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminFeesService : IAdminFeesService
    {
        private readonly IFeesConfigService _feesConfigService;
        private readonly VoltyksDbContext _context;

        public AdminFeesService(IFeesConfigService feesConfigService, VoltyksDbContext context)
        {
            _feesConfigService = feesConfigService;
            _context = context;
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
                // Validate amount
                if (dto.Amount <= 0)
                {
                    return new ApiResponse<object>(
                        message: "Amount must be greater than zero",
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

                // Update user wallet
                var currentWallet = recipientUser.Wallet ?? 0;
                var newWallet = currentWallet + (double)dto.Amount;
                recipientUser.Wallet = newWallet;

                // Save changes to database
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new
                    {
                        recipientUserId = dto.RecipientUserId,
                        recipientName = $"{recipientUser.FirstName} {recipientUser.LastName}",
                        amount = dto.Amount,
                        previousWallet = currentWallet,
                        newWallet = newWallet,
                        notes = dto.Notes,
                        transferredAt = DateTime.UtcNow
                    },
                    message: "Fees transferred successfully",
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
    }
}
