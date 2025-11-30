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

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

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
                if (dto.Content.ValueKind == JsonValueKind.Undefined)
                {
                    return new ApiResponse<object>(
                        message: "Content cannot be null",
                        status: false);
                }

                // Find existing terms for this language (get the latest/active one)
                var existingTerms = await _context.Set<TermsDocument>()
                    .Where(x => x.Lang == lang)
                    .OrderByDescending(x => x.IsActive)
                    .ThenByDescending(x => x.VersionNumber)
                    .FirstOrDefaultAsync(ct);

                // Get raw JSON exactly as received - no transformation
                string contentJson = dto.Content.GetRawText();

                if (existingTerms != null)
                {
                    // Update existing record
                    existingTerms.PayloadJson = contentJson;
                    existingTerms.PublishedAt = DateTime.UtcNow;
                    existingTerms.IsActive = true;

                    await _context.SaveChangesAsync(ct);

                    return new ApiResponse<object>(
                        data: new { Version = existingTerms.VersionNumber, Lang = lang, PublishedAt = existingTerms.PublishedAt },
                        message: "Terms updated successfully",
                        status: true);
                }
                else
                {
                    // Create new terms document only if none exists
                    var newTerms = new TermsDocument
                    {
                        VersionNumber = 1,
                        Lang = lang,
                        IsActive = true,
                        PublishedAt = DateTime.UtcNow,
                        PayloadJson = contentJson
                    };

                    _context.Set<TermsDocument>().Add(newTerms);
                    await _context.SaveChangesAsync(ct);

                    return new ApiResponse<object>(
                        data: new { Version = newTerms.VersionNumber, Lang = lang, PublishedAt = newTerms.PublishedAt },
                        message: "Terms created successfully",
                        status: true);
                }
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
