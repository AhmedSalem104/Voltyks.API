using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Protocol;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminProtocolService : IAdminProtocolService
    {
        private readonly VoltyksDbContext _context;

        public AdminProtocolService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<AdminProtocolDto>> GetProtocolAsync(CancellationToken ct = default)
        {
            try
            {
                // Read-only access to protocol document
                var protocol = await _context.termsDocuments
                    .AsNoTracking()
                    .Where(x => x.IsActive && x.Lang == "en")
                    .OrderByDescending(x => x.VersionNumber)
                    .FirstOrDefaultAsync(ct);

                if (protocol == null)
                {
                    return new ApiResponse<AdminProtocolDto>("Protocol not found", false);
                }

                var protocolDto = new AdminProtocolDto
                {
                    Version = protocol.VersionNumber,
                    Content = protocol.PayloadJson,
                    UpdatedAt = protocol.PublishedAt
                };

                return new ApiResponse<AdminProtocolDto>(protocolDto, "Protocol retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminProtocolDto>(
                    message: "Failed to retrieve protocol",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}
