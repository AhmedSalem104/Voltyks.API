using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.VehicleAdditionRequests;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.SignalR;
using Voltyks.Core.Constants;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.Enums;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminVehicleAdditionRequestsService : IAdminVehicleAdditionRequestsService
    {
        private readonly VoltyksDbContext _context;
        private readonly ISignalRService _signalRService;
        private readonly IFirebaseService _firebaseService;

        public AdminVehicleAdditionRequestsService(
            VoltyksDbContext context,
            ISignalRService signalRService,
            IFirebaseService firebaseService)
        {
            _context = context;
            _signalRService = signalRService;
            _firebaseService = firebaseService;
        }

        public async Task<ApiResponse<PagedResult<AdminVehicleAdditionRequestDto>>> GetAllAsync(
            string? status, PaginationParams? pagination, CancellationToken ct = default)
        {
            try
            {
                pagination ??= new PaginationParams();

                var query = _context.VehicleAdditionRequests
                    .AsNoTracking()
                    .Include(r => r.User)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(r => r.Status == status);

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .Select(r => new AdminVehicleAdditionRequestDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserFullName = r.User != null ? (r.User.FullName ?? r.User.UserName ?? "") : "",
                        UserEmail = r.User != null ? r.User.Email : null,
                        UserPhone = r.User != null ? r.User.PhoneNumber : null,
                        BrandName = r.BrandName,
                        ModelName = r.ModelName,
                        Capacity = r.Capacity,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        ProcessedBy = r.ProcessedBy
                    })
                    .ToListAsync(ct);

                var paged = new PagedResult<AdminVehicleAdditionRequestDto>(
                    items, totalCount, pagination.PageNumber, pagination.PageSize);

                return new ApiResponse<PagedResult<AdminVehicleAdditionRequestDto>>(
                    paged, $"Retrieved {items.Count} requests", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<AdminVehicleAdditionRequestDto>>(
                    message: "Failed to retrieve requests",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminVehicleAdditionRequestDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var dto = await _context.VehicleAdditionRequests
                    .AsNoTracking()
                    .Include(r => r.User)
                    .Where(r => r.Id == id)
                    .Select(r => new AdminVehicleAdditionRequestDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserFullName = r.User != null ? (r.User.FullName ?? r.User.UserName ?? "") : "",
                        UserEmail = r.User != null ? r.User.Email : null,
                        UserPhone = r.User != null ? r.User.PhoneNumber : null,
                        BrandName = r.BrandName,
                        ModelName = r.ModelName,
                        Capacity = r.Capacity,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        ProcessedBy = r.ProcessedBy
                    })
                    .FirstOrDefaultAsync(ct);

                if (dto is null)
                    return new ApiResponse<AdminVehicleAdditionRequestDto>("Request not found", status: false);

                return new ApiResponse<AdminVehicleAdditionRequestDto>(dto, "Request retrieved", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminVehicleAdditionRequestDto>(
                    message: "Failed to retrieve request",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> AcceptAsync(int id, string adminId, CancellationToken ct = default)
        {
            using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var request = await _context.VehicleAdditionRequests
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (request is null)
                    return new ApiResponse<object>("Request not found", status: false);

                if (request.Status != VehicleAdditionRequestStatuses.Pending)
                    return new ApiResponse<object>(
                        $"Request is already {request.Status}, cannot accept it.", status: false);

                // Re-check Brand+Model doesn't exist (race condition guard)
                var brandLower = request.BrandName.Trim().ToLower();
                var modelLower = request.ModelName.Trim().ToLower();

                var modelExists = await _context.Models
                    .AnyAsync(m =>
                        m.Name.ToLower() == modelLower &&
                        m.Brand != null &&
                        m.Brand.Name.ToLower() == brandLower, ct);

                if (modelExists)
                    return new ApiResponse<object>(
                        "Brand+Model already exists. Please decline this request.", status: false);

                // Parse capacity string -> double
                var match = Regex.Match(request.Capacity, @"[\d]+(\.[\d]+)?");
                if (!match.Success || !double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var capacityValue))
                    return new ApiResponse<object>(
                        $"Invalid capacity format: '{request.Capacity}'. Expected numeric value.", status: false);

                // Find or create Brand (case-insensitive)
                var brand = await _context.Brands
                    .FirstOrDefaultAsync(b => b.Name.ToLower() == brandLower, ct);

                if (brand is null)
                {
                    brand = new Brand { Name = request.BrandName.Trim() };
                    _context.Brands.Add(brand);
                    await _context.SaveChangesAsync(ct);
                }

                // Create Model
                var model = new Model
                {
                    Name = request.ModelName.Trim(),
                    BrandId = brand.Id,
                    Capacity = capacityValue
                };
                _context.Models.Add(model);
                await _context.SaveChangesAsync(ct);

                // Update request status
                request.Status = VehicleAdditionRequestStatuses.Accepted;
                request.UpdatedAt = DateTime.UtcNow;
                request.ProcessedBy = adminId;
                await _context.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);

                // Notify user (DB + SignalR, no FCM)
                await NotifyUserAsync(
                    userId: request.UserId,
                    requestId: request.Id,
                    title: "Request Accepted",
                    body: "Your vehicle is now available! You can add your vehicle now with us",
                    type: NotificationTypes.VehicleAdditionRequest_Accepted,
                    ct: ct);

                return new ApiResponse<object>(
                    new { requestId = request.Id, brandId = brand.Id, modelId = model.Id },
                    "Request accepted successfully. Vehicle added to the system.",
                    true);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>(
                    message: "Failed to accept request",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> DeclineAsync(int id, string adminId, CancellationToken ct = default)
        {
            try
            {
                var request = await _context.VehicleAdditionRequests
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (request is null)
                    return new ApiResponse<object>("Request not found", status: false);

                if (request.Status != VehicleAdditionRequestStatuses.Pending)
                    return new ApiResponse<object>(
                        $"Request is already {request.Status}, cannot decline it.", status: false);

                request.Status = VehicleAdditionRequestStatuses.Declined;
                request.UpdatedAt = DateTime.UtcNow;
                request.ProcessedBy = adminId;
                await _context.SaveChangesAsync(ct);

                // Notify user (DB + SignalR, no FCM)
                await NotifyUserAsync(
                    userId: request.UserId,
                    requestId: request.Id,
                    title: "Request Declined",
                    body: "The vehicle you requested already exists. Please check again",
                    type: NotificationTypes.VehicleAdditionRequest_Declined,
                    ct: ct);

                return new ApiResponse<object>(
                    new { requestId = request.Id },
                    "Request declined successfully.",
                    true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to decline request",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        private async Task NotifyUserAsync(
            string userId, int requestId, string title, string body, string type, CancellationToken ct)
        {
            // 1) Persist to DB
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                OriginalId = requestId,
                UserTypeId = 2, // VehicleOwner
                IsAdminNotification = false,
                IsRead = false,
                SentAt = DateTime.UtcNow,
                RelatedRequestId = null
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(ct);

            // 2) FCM push (works even if app is closed)
            try
            {
                var tokens = await _context.Set<DeviceToken>()
                    .AsNoTracking()
                    .Where(t => t.UserId == userId && !string.IsNullOrEmpty(t.Token))
                    .Select(t => t.Token)
                    .ToListAsync(ct);

                if (tokens.Count > 0)
                {
                    var extraData = new Dictionary<string, string>
                    {
                        ["vehicleAdditionRequestId"] = requestId.ToString(),
                        ["NotificationType"] = type,
                        ["userRole"] = "vehicle_owner"
                    };

                    await Task.WhenAll(tokens.Select(t =>
                        _firebaseService.SendNotificationAsync(t, title, body, requestId, type, extraData)
                    ));
                }
            }
            catch
            {
                // FCM failure shouldn't block the accept/decline flow
            }

            // 3) Real-time via SignalR (if user connected)
            try
            {
                await _signalRService.SendNotificationAsync(userId, title, body, new
                {
                    id = notification.Id,
                    type,
                    vehicleAdditionRequestId = requestId,
                    userRole = "vehicle_owner"
                }, ct);
            }
            catch
            {
                // SignalR failure shouldn't block the accept/decline flow
            }
        }
    }
}
