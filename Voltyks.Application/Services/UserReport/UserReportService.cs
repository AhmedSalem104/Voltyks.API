using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Application.Interfaces.UserReport;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.DTOs.Report;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using UserReportEntity = Voltyks.Persistence.Entities.Main.UserReport;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Voltyks.Application.Services.UserReport
{
    public class UserReportService : IUserReportService
    {
        private readonly VoltyksDbContext _ctx;
        private readonly IMapper _mapper;

        public UserReportService(VoltyksDbContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        // إنشاء تقرير
        public async Task<ApiResponse<object>> CreateReportAsync(ReportDto dto, CancellationToken ct = default)
        {
            var report = new UserReportEntity
            {
                ProcessId = dto.ProcessId,
                UserId = dto.UserId,
                ReportDate = DateTime.UtcNow,
                IsResolved = dto.IsResolved
            };

            await _ctx.UserReports.AddAsync(report, ct);
            await _ctx.SaveChangesAsync(ct);

            return new ApiResponse<object>(new { message = "Report created successfully", reportId = report.Id });
        }

        // الحصول على جميع التقارير بناءً على الفلترة
        public async Task<ApiResponse<List<ReportDto>>> GetReportsAsync(ReportFilterDto filter, CancellationToken ct = default)
        {
            var query = _ctx.UserReports.AsQueryable();

            // فلترة حسب المستخدم (اختياري)
            if (!string.IsNullOrEmpty(filter.UserId))
            {
                query = query.Where(r => r.UserId == filter.UserId);
            }

            // فلترة حسب التاريخ (اختياري)
            if (filter.StartDate.HasValue)
            {
                query = query.Where(r => r.ReportDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(r => r.ReportDate <= filter.EndDate.Value);
            }

            var reports = await query.ToListAsync(ct);
            var reportDtos = _mapper.Map<List<ReportDto>>(reports);

            return new ApiResponse<List<ReportDto>>(reportDtos, "Reports retrieved successfully", true);


        }

        // الحصول على تقرير معين حسب الـ ReportId
        public async Task<ApiResponse<ReportDto>> GetReportByIdAsync(int reportId, CancellationToken ct = default)
        {
            var report = await _ctx.UserReports
                .Include(r => r.User)
                .Include(r => r.Process)
                .FirstOrDefaultAsync(r => r.Id == reportId, ct);

            if (report == null)
                return new ApiResponse<ReportDto>("Report not found", false);

            var reportDetails = new ReportDto
            {
                ReportId = report.Id,
                ProcessId = report.ProcessId,
                UserId = report.UserId,
                ReportDate = report.ReportDate,
                IsResolved = report.IsResolved,
                UserDetails = new UserDetailDto
                {
                    FullName = report.User.FullName,
                    Email = report.User.Email,
                    Phone = report.User.PhoneNumber
                }
            };

            return new ApiResponse<ReportDto>(reportDetails, "Report details retrieved", true);
        }
    }

}
