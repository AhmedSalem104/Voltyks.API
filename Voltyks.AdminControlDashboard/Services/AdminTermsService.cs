using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Terms;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Application.Interfaces.Terms;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminTermsService : IAdminTermsService
    {
        private readonly ITermsService _termsService;
        private readonly VoltyksDbContext _context;

        public AdminTermsService(ITermsService termsService, VoltyksDbContext context)
        {
            _termsService = termsService;
            _context = context;
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
                // Validate and normalize language
                var lang = (dto.Lang?.ToLowerInvariant()) switch
                {
                    "ar" => "ar",
                    _ => "en"
                };

                // Validate content
                if (dto.Content == null)
                {
                    return new ApiResponse<object>(
                        message: "Content cannot be null",
                        status: false);
                }

                // Deactivate current active terms for this language
                var activeTerms = await _context.Set<TermsDocument>()
                    .Where(x => x.IsActive && x.Lang == lang)
                    .ToListAsync(ct);

                foreach (var term in activeTerms)
                {
                    term.IsActive = false;
                }

                // Get the next version number
                var maxVersion = await _context.Set<TermsDocument>()
                    .Where(x => x.Lang == lang)
                    .MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0;

                var newVersion = maxVersion + 1;

                // Serialize content to JSON
                var contentJson = JsonSerializer.Serialize(dto.Content);

                // Create new terms document
                var newTerms = new TermsDocument
                {
                    VersionNumber = newVersion,
                    Lang = lang,
                    IsActive = true,
                    PublishedAt = DateTime.UtcNow,
                    PayloadJson = contentJson
                };

                _context.Set<TermsDocument>().Add(newTerms);

                // Save changes to database
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { Version = newVersion, Lang = lang, PublishedAt = newTerms.PublishedAt },
                    message: "Terms updated successfully",
                    status: true);
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
