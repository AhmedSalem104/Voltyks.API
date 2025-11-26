using Voltyks.AdminControlDashboard.Dtos.Terms;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Application.Interfaces.Terms;
using Voltyks.Core.DTOs;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminTermsService : IAdminTermsService
    {
        private readonly ITermsService _termsService;

        public AdminTermsService(ITermsService termsService, Persistence.Data.VoltyksDbContext context)
        {
            _termsService = termsService;
        }

        public async Task<ApiResponse<AdminTermsDto>> GetTermsAsync(string lang = "en", CancellationToken ct = default)
        {
            try
            {
                // Pure wrapper - call existing service
                var result = await _termsService.GetAsync(lang, null, ct);

                if (result == null)
                {
                    return new ApiResponse<AdminTermsDto>("Terms not found", false);
                }

                var adminDto = new AdminTermsDto
                {
                    Version = result.Version,
                    Lang = result.Lang,
                    PublishedAt = result.PublishedAt,
                    Content = result.Content
                };

                return new ApiResponse<AdminTermsDto>(adminDto, "Terms retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminTermsDto>(
                    message: "Failed to retrieve terms",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> UpdateTermsAsync(UpdateTermsDto dto, CancellationToken ct = default)
        {
            try
            {
                // This would need implementation in the existing service
                // For now, return not implemented
                return new ApiResponse<object>(
                    message: "Update terms functionality requires implementation in TermsService",
                    status: false);
            }
            catch (Exception ex)
            {

                return new ApiResponse<object>(
                    message: "Failed to update terms",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}
