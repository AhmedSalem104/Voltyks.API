using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voltyks.AdminControlDashboard.Dtos.Users;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Persistence.Entities.Main.Paymob;
using Voltyks.Persistence.Entities.Main.Store;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminUsersService : IAdminUsersService
    {
        private readonly VoltyksDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminUsersService(
            VoltyksDbContext context,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<ApiResponse<List<AdminUserDto>>> GetUsersAsync(string? search = null, bool includeDeleted = false, CancellationToken ct = default)
        {
            try
            {
                // DbContext for complex query (same pattern as ProcessesService)
                var query = _context.Users.AsNoTracking();

                // Filter out deleted users unless includeDeleted is true
                if (!includeDeleted)
                {
                    query = query.Where(u => !u.IsDeleted);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(u =>
                        u.FullName.Contains(search) ||
                        u.Email.Contains(search) ||
                        u.PhoneNumber.Contains(search));
                }

                var users = await query
                    .OrderByDescending(u => u.Id)
                    .Take(50) // Same pattern as ProcessesService
                    .Select(u => new AdminUserDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        IsBanned = u.IsBanned,
                        IsAvailable = u.IsAvailable,
                        Rating = u.Rating,
                        IsDeleted = u.IsDeleted,
                        DeletedAt = u.DeletedAt,
                        DateCreated = DateTime.Now // AppUser doesn't have DateCreated, using Now as placeholder
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminUserDto>>(users, "Users retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminUserDto>>(
                    message: "Failed to retrieve users",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminUserDetailsDto>> GetUserByIdAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                // DbContext for complex query with includes
                var user = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Chargers)
                    .Include(u => u.ChargingRequests)
                    .FirstOrDefaultAsync(u => u.Id == userId, ct);

                if (user == null)
                {
                    return new ApiResponse<AdminUserDetailsDto>("User not found", false);
                }

                var userDto = new AdminUserDetailsDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    NationalId = user.NationalId,
                    IsBanned = user.IsBanned,
                    IsAvailable = user.IsAvailable,
                    Rating = user.Rating,
                    RatingCount = user.RatingCount,
                    Wallet = user.Wallet,
                    DateCreated = DateTime.Now,
                    TotalChargers = user.Chargers?.Count ?? 0,
                    TotalVehicles = 0, // Will need to query separately if needed
                    TotalChargingRequests = user.ChargingRequests?.Count ?? 0
                };

                return new ApiResponse<AdminUserDetailsDto>(userDto, "User details retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminUserDetailsDto>(
                    message: "Failed to retrieve user details",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> ToggleBanUserAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                // DbContext for simple update (same pattern as ProcessesService)
                var user = await _context.Users.FindAsync(new object[] { userId }, ct);

                if (user == null)
                {
                    return new ApiResponse<object>("User not found", false);
                }

                user.IsBanned = !user.IsBanned;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { userId, isBanned = user.IsBanned },
                    message: $"User {(user.IsBanned ? "banned" : "unbanned")} successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to toggle ban status",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminWalletDto>> GetUserWalletAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId, ct);

                if (user == null)
                {
                    return new ApiResponse<AdminWalletDto>("User not found", false);
                }

                var walletDto = new AdminWalletDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    WalletBalance = user.Wallet,
                    LastUpdated = DateTime.Now
                };

                return new ApiResponse<AdminWalletDto>(walletDto, "Wallet retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminWalletDto>(
                    message: "Failed to retrieve wallet",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<AdminUserVehicleDto>>> GetUserVehiclesAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                var vehicles = await _context.Set<Voltyks.Persistence.Entities.Main.Vehicle>()
                    .AsNoTracking()
                    .Include(v => v.Brand)
                    .Include(v => v.Model)
                    .Where(v => v.UserId == userId && !v.IsDeleted)
                    .Select(v => new AdminUserVehicleDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Plate = v.Plate,
                        Year = v.Year,
                        CreationDate = v.CreationDate,
                        BrandName = v.Brand.Name,
                        ModelName = v.Model.Name,
                        ModelCapacity = v.Model.Capacity,
                        IsDeleted = v.IsDeleted
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminUserVehicleDto>>(vehicles, "Vehicles retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminUserVehicleDto>>(
                    message: "Failed to retrieve vehicles",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<AdminUserReportDto>>> GetUserReportsAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                var reports = await _context.Set<Voltyks.Persistence.Entities.Main.UserReport>()
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.ReportDate)
                    .Select(r => new AdminUserReportDto
                    {
                        Id = r.Id,
                        ProcessId = r.ProcessId,
                        ReportDate = r.ReportDate,
                        ReportContent = r.ReportContent,
                        IsResolved = r.IsResolved
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminUserReportDto>>(reports, "Reports retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminUserReportDto>>(
                    message: "Failed to retrieve reports",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> SoftDeleteUserAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                var user = await _context.Users.FindAsync(new object[] { userId }, ct);

                if (user == null)
                {
                    return new ApiResponse<object>("User not found", false);
                }

                // Prevent deleting Admin user
                if (user.UserName == "Admin")
                {
                    return new ApiResponse<object>("Cannot delete Admin user", false);
                }

                // Prevent deleting yourself
                var currentUserId = GetCurrentUserId();
                if (userId == currentUserId)
                {
                    return new ApiResponse<object>("Cannot delete yourself", false);
                }

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { userId, isDeleted = true, deletedAt = user.DeletedAt },
                    message: "User deleted successfully (soft delete)",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to delete user",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> HardDeleteUserAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Chargers)
                    .Include(u => u.ChargingRequests)
                    .Include(u => u.DeviceTokens)
                    .Include(u => u.Notifications)
                    .Include(u => u.WalletTransactions)
                    .FirstOrDefaultAsync(u => u.Id == userId, ct);

                if (user == null)
                {
                    return new ApiResponse<object>("User not found", false);
                }

                // Prevent deleting Admin user
                if (user.UserName == "Admin")
                {
                    return new ApiResponse<object>("Cannot delete Admin user", false);
                }

                // Prevent deleting yourself
                var currentUserId = GetCurrentUserId();
                if (userId == currentUserId)
                {
                    return new ApiResponse<object>("Cannot delete yourself", false);
                }

                // ========== DELETE RELATED ENTITIES ==========

                // 1. Delete DeviceTokens
                if (user.DeviceTokens?.Any() == true)
                    _context.Set<DeviceToken>().RemoveRange(user.DeviceTokens);

                // 2. Delete Notifications
                if (user.Notifications?.Any() == true)
                    _context.Set<Notification>().RemoveRange(user.Notifications);

                // 3. Delete WalletTransactions
                if (user.WalletTransactions?.Any() == true)
                    _context.Set<WalletTransaction>().RemoveRange(user.WalletTransactions);

                // 4. Delete Vehicles
                var vehicles = await _context.Set<Vehicle>()
                    .Where(v => v.UserId == userId)
                    .ToListAsync(ct);
                if (vehicles.Any())
                    _context.Set<Vehicle>().RemoveRange(vehicles);

                // 5. Delete UserGeneralComplaints
                var complaints = await _context.Set<UserGeneralComplaint>()
                    .Where(c => c.UserId == userId)
                    .ToListAsync(ct);
                if (complaints.Any())
                    _context.Set<UserGeneralComplaint>().RemoveRange(complaints);

                // 6. Delete UserReports
                var reports = await _context.Set<UserReport>()
                    .Where(r => r.UserId == userId)
                    .ToListAsync(ct);
                if (reports.Any())
                    _context.Set<UserReport>().RemoveRange(reports);

                // ========== NEW: MISSING ENTITIES ==========

                // 7. Delete ChargingRequests made BY the user
                if (user.ChargingRequests?.Any() == true)
                    _context.Set<ChargingRequest>().RemoveRange(user.ChargingRequests);

                // 8. Delete ChargingRequests ON user's chargers (must delete before chargers)
                if (user.Chargers?.Any() == true)
                {
                    var chargerIds = user.Chargers.Select(c => c.Id).ToList();
                    var requestsOnUserChargers = await _context.Set<ChargingRequest>()
                        .Where(r => chargerIds.Contains(r.ChargerId))
                        .ToListAsync(ct);
                    if (requestsOnUserChargers.Any())
                        _context.Set<ChargingRequest>().RemoveRange(requestsOnUserChargers);

                    // 9. Delete Chargers (must delete after their requests)
                    _context.Set<Charger>().RemoveRange(user.Chargers);
                }

                // 10. Delete StoreReservations
                var reservations = await _context.Set<StoreReservation>()
                    .Where(r => r.UserId == userId)
                    .ToListAsync(ct);
                if (reservations.Any())
                    _context.Set<StoreReservation>().RemoveRange(reservations);

                // 11. Delete RatingsHistory (as Rater and Ratee)
                var ratingsAsRater = await _context.Set<RatingsHistory>()
                    .Where(r => r.RaterUserId == userId)
                    .ToListAsync(ct);
                if (ratingsAsRater.Any())
                    _context.Set<RatingsHistory>().RemoveRange(ratingsAsRater);

                var ratingsAsRatee = await _context.Set<RatingsHistory>()
                    .Where(r => r.RateeUserId == userId)
                    .ToListAsync(ct);
                if (ratingsAsRatee.Any())
                    _context.Set<RatingsHistory>().RemoveRange(ratingsAsRatee);

                // 12. Delete UsersBanned
                var bans = await _context.Set<UsersBanned>()
                    .Where(b => b.UserId == userId)
                    .ToListAsync(ct);
                if (bans.Any())
                    _context.Set<UsersBanned>().RemoveRange(bans);

                // 13. Delete PaymentOrders
                var paymentOrders = await _context.Set<PaymentOrder>()
                    .Where(p => p.UserId == userId)
                    .ToListAsync(ct);
                if (paymentOrders.Any())
                    _context.Set<PaymentOrder>().RemoveRange(paymentOrders);

                // 14. Delete UserSavedCards
                var savedCards = await _context.Set<UserSavedCard>()
                    .Where(c => c.UserId == userId)
                    .ToListAsync(ct);
                if (savedCards.Any())
                    _context.Set<UserSavedCard>().RemoveRange(savedCards);

                // 15. Delete RevokedCardTokens
                var revokedTokens = await _context.Set<RevokedCardToken>()
                    .Where(t => t.UserId == userId)
                    .ToListAsync(ct);
                if (revokedTokens.Any())
                    _context.Set<RevokedCardToken>().RemoveRange(revokedTokens);

                // 16. Delete CardTokenWebhookLogs (nullable UserId)
                var webhookLogs = await _context.Set<CardTokenWebhookLog>()
                    .Where(l => l.UserId == userId)
                    .ToListAsync(ct);
                if (webhookLogs.Any())
                    _context.Set<CardTokenWebhookLog>().RemoveRange(webhookLogs);

                // ========== FINALLY DELETE USER ==========
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { userId, permanentlyDeleted = true },
                    message: "User permanently deleted",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to permanently delete user. User may have related data that prevents deletion.",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> RestoreUserAsync(string userId, CancellationToken ct = default)
        {
            try
            {
                var user = await _context.Users.FindAsync(new object[] { userId }, ct);

                if (user == null)
                {
                    return new ApiResponse<object>("User not found", false);
                }

                if (!user.IsDeleted)
                {
                    return new ApiResponse<object>("User is not deleted", false);
                }

                user.IsDeleted = false;
                user.DeletedAt = null;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { userId, isDeleted = false, restored = true },
                    message: "User restored successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to restore user",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}
