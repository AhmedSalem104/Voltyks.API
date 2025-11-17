using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voltyks.AdminControlDashboard.Dtos.Reports;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminReportsService : IAdminReportsService
    {
        private readonly VoltyksDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminReportsService(
            VoltyksDbContext context,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<ApiResponse<List<AdminReportDto>>> GetReportsAsync(
            AdminReportFilterDto? filter = null,
            CancellationToken ct = default)
        {
            try
            {
                // Same pattern as UserReportService - simple filter without pagination
                IQueryable<Voltyks.Persistence.Entities.Main.UserReport> query = _context.Set<Voltyks.Persistence.Entities.Main.UserReport>()
                    .AsNoTracking()
                    .Include(r => r.User);

                if (filter != null)
                {
                    if (!string.IsNullOrEmpty(filter.UserId))
                    {
                        query = query.Where(r => r.UserId == filter.UserId);
                    }

                    if (filter.StartDate.HasValue)
                    {
                        query = query.Where(r => r.ReportDate >= filter.StartDate.Value);
                    }

                    if (filter.EndDate.HasValue)
                    {
                        query = query.Where(r => r.ReportDate <= filter.EndDate.Value);
                    }

                    if (filter.IsResolved.HasValue)
                    {
                        query = query.Where(r => r.IsResolved == filter.IsResolved.Value);
                    }
                }

                var reports = await query
                    .OrderByDescending(r => r.ReportDate)
                    .Take(50)
                    .Select(r => new AdminReportDto
                    {
                        Id = r.Id,
                        ProcessId = r.ProcessId,
                        UserId = r.UserId,
                        UserFullName = r.User.FullName,
                        ReportDate = r.ReportDate,
                        ReportContent = r.ReportContent,
                        IsResolved = r.IsResolved
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminReportDto>>(reports, "Reports retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminReportDto>>(
                    message: "Failed to retrieve reports",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminReportDetailsDto>> GetReportByIdAsync(int reportId, CancellationToken ct = default)
        {
            try
            {
                var report = await _context.Set<Voltyks.Persistence.Entities.Main.UserReport>()
                    .AsNoTracking()
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == reportId, ct);

                if (report == null)
                {
                    return new ApiResponse<AdminReportDetailsDto>("Report not found", false);
                }

                var reportDto = new AdminReportDetailsDto
                {
                    Id = report.Id,
                    ProcessId = report.ProcessId,
                    UserId = report.UserId,
                    UserFullName = report.User?.FullName,
                    UserEmail = report.User?.Email,
                    UserPhone = report.User?.PhoneNumber,
                    ReportDate = report.ReportDate,
                    ReportContent = report.ReportContent,
                    IsResolved = report.IsResolved
                };

                return new ApiResponse<AdminReportDetailsDto>(reportDto, "Report details retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminReportDetailsDto>(
                    message: "Failed to retrieve report details",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}
