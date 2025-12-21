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
using Voltyks.Core.Enums;
using Voltyks.Core.DTOs.ChargerRequest;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Utilities;
using Voltyks.Application.Interfaces.SignalR;

namespace Voltyks.Application.Services.UserReport
{
    public class UserReportService : IUserReportService
    {
        private readonly VoltyksDbContext _ctx;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IFirebaseService _firebase;
        private readonly ISignalRService _signalRService;

        public UserReportService(VoltyksDbContext ctx, IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IFirebaseService firebase, ISignalRService signalRService)
        {
            _ctx = ctx;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _httpContext = httpContextAccessor;
            _firebase = firebase;
            _signalRService = signalRService;
        }

        public async Task<ApiResponse<object>> CreateReportAsync(ReportDataDto dto, CancellationToken ct = default)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return new ApiResponse<object>(new { message = "User not found" }, "User not found", false);

            var process = await _unitOfWork.GetRepository<Process, int>().GetAsync(dto.ProcessId);
            if (process == null)
                return new ApiResponse<object>(new { message = "Process not found" }, "Process not found", false);

            var report = new UserReportEntity
            {
                ProcessId = dto.ProcessId,
                UserId = user.Id,
                ReportDate = DateTime.UtcNow,
                ReportContent = dto.ReportContent,
                IsResolved = false,
                User = user,
                Process = process
            };

            await _ctx.UserReports.AddAsync(report, ct);
            await _ctx.SaveChangesAsync(ct);

            // ===== Admin SignalR Notification (Real-time) =====
            var adminNotification = new Notification
            {
                Title = "بلاغ جديد",
                Body = $"تم إنشاء بلاغ جديد من {user.FullName ?? user.UserName ?? "مستخدم"}",
                IsRead = false,
                SentAt = DateTimeHelper.GetEgyptTime(),
                UserId = null,
                Type = NotificationTypes.Admin_Report_Created,
                OriginalId = report.Id,
                IsAdminNotification = true,
                UserTypeId = 0
            };
            await _ctx.Notifications.AddAsync(adminNotification, ct);
            await _ctx.SaveChangesAsync(ct);

            // Broadcast to Admin Dashboard via SignalR
            await _signalRService.SendBroadcastAsync(
                "بلاغ جديد",
                $"تم إنشاء بلاغ جديد من {user.FullName ?? user.UserName ?? "مستخدم"}",
                new
                {
                    id = $"report_{report.Id}",
                    type = "report",
                    originalId = report.Id,
                    title = "بلاغ جديد",
                    message = $"تم إنشاء بلاغ جديد من {user.FullName ?? user.UserName ?? "مستخدم"}",
                    userName = user.FullName ?? user.UserName ?? "",
                    timestamp = adminNotification.SentAt.ToString("O")
                },
                ct
            );

            // ===== إشعار للطرف المقابل فقط بعنوان ديناميكي =====
            var reporterName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName
                             : !string.IsNullOrWhiteSpace(user.UserName) ? user.UserName
                             : "Someone";

            var isReporterVehicleOwner = process.VehicleOwnerId == user.Id;
            var receiverUserId = isReporterVehicleOwner ? process.ChargerOwnerId : process.VehicleOwnerId;

            var notificationType = isReporterVehicleOwner
                ? NotificationTypes.Report_VehicleOwnerToChargerOwner
                : NotificationTypes.Report_ChargerOwnerToVehicleOwner;

            var title = $"{reporterName} filed a report against you";
            var body = "Open the process to review the report details.";

            // data الإضافية داخل الـ push
            var extraData = new Dictionary<string, string>
            {
                ["reportId"] = report.Id.ToString(),
                ["reporterId"] = user.Id,
                ["reporterName"] = reporterName
            };

            var notifDto = await SendAndPersistNotificationAsync(
                receiverUserId: receiverUserId,
                requestId: process.ChargerRequestId,
                title: title,
                processId: process.Id,
                body: body,
                notificationType: notificationType,
                userTypeId: isReporterVehicleOwner ? 1 : 2, // 1=ChargerOwner, 2=VehicleOwner (المستلم)
                ct: ct,
                extraData: extraData
            );

            // ✅ نفس شكل create & update: data = notification + شوية فيلدز زيادة
            var responseData = new
            {
                notificationId = notifDto.NotificationId,
                requestId = notifDto.RequestId,
                recipientUserId = notifDto.RecipientUserId,
                title = notifDto.Title,
                body = notifDto.Body,
                notificationType = notifDto.NotificationType,
                sentAt = notifDto.SentAt,
                pushSentCount = notifDto.PushSentCount,

                processId = process.Id,
                reportId = report.Id,
                reporterId = user.Id,
                reporterName = reporterName
            };

            return new ApiResponse<object>(responseData, "Report created successfully", true);
        }


        // الحصول على جميع التقارير بناءً على الفلترة
        public async Task<ApiResponse<List<ReportDto>>> GetReportsAsync(ReportFilterDto filter, CancellationToken ct = default)
        {
            var query = _ctx.UserReports.AsNoTracking().AsQueryable();

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
                .AsNoTracking()
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
        private async Task<Notification> AddNotificationAsync(
  string receiverUserId,
  int relatedRequestId,
  string title,
  string body,
  int userTypeId,
  CancellationToken ct)
        {
            var notification = new Notification
            {
                Title = title,
                Body = body,
                IsRead = false,
                SentAt = DateTimeHelper.GetEgyptTime(),
                UserId = receiverUserId,
                RelatedRequestId = relatedRequestId,
                UserTypeId = userTypeId

            };

            await _ctx.AddAsync(notification, ct);
            await _ctx.SaveChangesAsync(ct);
            return notification;
        }
        private async Task<NotificationResultDto> SendAndPersistNotificationAsync(
      string receiverUserId,
      int requestId,
      string title,
      int processId,
      string body,
      string notificationType,
      int userTypeId,
      CancellationToken ct,
      Dictionary<string, string>? extraData = null // NEW
  )
        {
            var data = new Dictionary<string, string>
            {
                ["NotificationType"] = notificationType,
                ["requestId"] = requestId.ToString(),
                ["processId"] = processId.ToString()
            };
            if (extraData != null)
                foreach (var kv in extraData) data[kv.Key] = kv.Value;

            var tokens = await _ctx.Set<DeviceToken>()
                                   .Where(t => t.UserId == receiverUserId && !string.IsNullOrEmpty(t.Token))
                                   .Select(t => t.Token)
                                   .ToListAsync(ct);

            if (tokens.Count > 0)
                await Task.WhenAll(tokens.Select(tk =>
                    _firebase.SendNotificationAsync(tk, title, body, requestId, notificationType, data)
                ));

            var notification = await AddNotificationAsync(receiverUserId, requestId, title, body, userTypeId, ct);

            return new NotificationResultDto(
                NotificationId: notification.Id,
                RequestId: requestId,
                RecipientUserId: receiverUserId,
                Title: title,
                Body: body,
                NotificationType: notificationType,
                SentAt: notification.SentAt,
                PushSentCount: tokens.Count
            );
        }
    }

}
