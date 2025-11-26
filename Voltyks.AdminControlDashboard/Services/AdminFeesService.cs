using Voltyks.AdminControlDashboard.Dtos.Fees;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Application.Interfaces.FeesConfig;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.FeesConfig;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminFeesService : IAdminFeesService
    {
        private readonly IFeesConfigService _feesConfigService;

        public AdminFeesService(IFeesConfigService feesConfigService, Persistence.Data.VoltyksDbContext context)
        {
            _feesConfigService = feesConfigService;
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
                // This would need to be implemented based on business logic
                // For now, return not implemented as this requires wallet transfer logic
                return new ApiResponse<object>(
                    message: "Transfer fees functionality not yet implemented",
                    status: false);
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
