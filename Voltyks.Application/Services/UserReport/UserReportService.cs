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
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Core.DTOs.Charger;

namespace Voltyks.Application.Services.UserReport
{
    public class UserReportService : IUserReportService
    {
        private readonly VoltyksDbContext _ctx;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContext;



        public UserReportService(VoltyksDbContext ctx, IMapper mapper ,IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _ctx = ctx;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _httpContext = httpContextAccessor;
        }

        // إنشاء تقرير
        //public async Task<ApiResponse<object>> CreateReportAsync(ReportDto dto, CancellationToken ct = default)
        //{
        //    //var report = new UserReportEntity
        //    //{
        //    //    ProcessId = dto.ProcessId,
        //    //    UserId = dto.UserId,
        //    //    ReportDate = DateTime.UtcNow,
        //    //    IsResolved = dto.IsResolved
        //    //};

        //    // إنشاء بيانات اختبارية
        //    var report = new UserReportEntity
        //    {
        //        ProcessId = dto.ProcessId,
        //        UserId = dto.UserId,
        //        ReportDate = DateTime.Now,  // سيتم تعيين التاريخ الحالي
        //        IsResolved = dto.IsResolved,
        //        User = _ctx.AppUsers.FirstOrDefault(u => u.Id == "878f90b5-e75e-4439-88a0-298636140ba7"),  // ربط المستخدم
        //        Process = _ctx.Processes.FirstOrDefault(p => p.Id == 100)   // ربط العملية

        //    };


        //    await _ctx.UserReports.AddAsync(report, ct);
        //    await _ctx.SaveChangesAsync(ct);

        //    return new ApiResponse<object>(new { message = "Report created successfully", reportId = report.Id });
        //}
        //public async Task<ApiResponse<object>> CreateReportAsync(ReportDataDto dto, CancellationToken ct = default)
        //{

        //    // التحقق من وجود المستخدم في قاعدة البيانات
        //    var user = await GetCurrentUserAsync();
        //   // var user = await _ctx.AppUsers.FirstOrDefaultAsync(u => u.Id == dto.UserId, cancellationToken: ct);
        //    if (user == null)
        //    {
        //        return new ApiResponse<object>(new { message = "User not found" });
        //    }

        //    // التحقق من وجود العملية في قاعدة البيانات
        //    var repo = _unitOfWork.GetRepository<Process, int>();

        //    // التحقق من وجود العملية في قاعدة البيانات باستخدام GetAsync
        //    var process = await _unitOfWork.GetRepository<Process, int>().GetAsync(dto.ProcessId);

        //    if (process == null)
        //    {
        //        return new ApiResponse<object>(new { message = "Process not found" });
        //    }


        //    var data = _mapper.Map<IEnumerable<ReportDto>>(dto);
        //    // إنشاء التقرير مع ربط المستخدمين والعمليات
        //    var report = new UserReportEntity
        //    {
        //        ProcessId = dto.ProcessId,
        //        UserId = user.Id,
        //        ReportDate = DateTime.UtcNow,  // استخدام التوقيت العالمي الموحد
        //        ReportContent = dto.ReportContent,
        //        IsResolved = false,
        //        User = user,  // ربط المستخدم
        //        Process = process  // ربط العملية
        //    };

        //    // إضافة التقرير إلى قاعدة البيانات
        //    await _ctx.UserReports.AddAsync(report, ct);
        //    await _ctx.SaveChangesAsync(ct);

        //    // إرجاع استجابة بنجاح التقرير
        //    return new ApiResponse<object>(new { message = "Report created successfully", reportId = report.Id });
        //}
        public async Task<ApiResponse<object>> CreateReportAsync(ReportDataDto dto, CancellationToken ct = default)
        {
            // التحقق من وجود المستخدم في قاعدة البيانات
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return new ApiResponse<object>(new { message = "User not found" });
            }

            // التحقق من وجود العملية في قاعدة البيانات
            var process = await _unitOfWork.GetRepository<Process, int>().GetAsync(dto.ProcessId);

            if (process == null)
            {
                return new ApiResponse<object>(new { message = "Process not found" });
            }

            // تحويل ReportDataDto إلى ReportDto باستخدام AutoMapper
            //var reportDto = _mapper.Map<ReportDto>(dto);

            // إنشاء التقرير مع ربط المستخدمين والعمليات
            var report = new UserReportEntity
            {
                ProcessId = dto.ProcessId,
                UserId = user.Id,
                ReportDate = DateTime.UtcNow,  // استخدام التوقيت العالمي الموحد
                ReportContent = dto.ReportContent,
                IsResolved = false,
                User = user,  // ربط المستخدم
                Process = process  // ربط العملية
            };

            // إضافة التقرير إلى قاعدة البيانات
            await _ctx.UserReports.AddAsync(report, ct);
            await _ctx.SaveChangesAsync(ct);

            // إرجاع استجابة بنجاح التقرير
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
                ProcessId = report.ProcessId,
                UserId = report.UserId,
                ReportDate = report.ReportDate,
                IsResolved = report.IsResolved,
                ReportContent = report.ReportContent,
                UserDetails = new UserDetailDto
                {
                    FullName = report.User.FullName,
                    Email = report.User.Email,
                    Phone = report.User.PhoneNumber
                }
            };

            return new ApiResponse<ReportDto>(reportDetails, "Report details retrieved", true);
        }
        private async Task<AppUser?> GetCurrentUserAsync()
        {
            // الحصول على الـ UserId من الـ Claims
            var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return null; // في حال عدم وجود UserId
            }

            // استرجاع كائن AppUser من قاعدة البيانات باستخدام الـ UserId
            var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == userId);  // إضافة await هنا

            return user;
        }

    }

}
