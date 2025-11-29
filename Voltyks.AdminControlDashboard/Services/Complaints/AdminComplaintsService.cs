using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Complaints;
using Voltyks.AdminControlDashboard.Interfaces.Complaints;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;

namespace Voltyks.AdminControlDashboard.Services.Complaints
{
    public class AdminComplaintsService : IAdminComplaintsService
    {
        private readonly VoltyksDbContext _context;

        public AdminComplaintsService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<AdminComplaintDto>>> GetAllComplaintsAsync(
            bool includeResolved = true,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.UserGeneralComplaints
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Include(c => c.Category)
                    .AsQueryable();

                if (!includeResolved)
                {
                    query = query.Where(c => !c.IsResolved);
                }

                var complaints = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new AdminComplaintDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = c.User != null ? c.User.UserName ?? "" : "",
                        UserEmail = c.User != null ? c.User.Email : null,
                        UserPhone = c.User != null ? c.User.PhoneNumber : null,
                        CategoryId = c.CategoryId,
                        CategoryName = c.Category != null ? c.Category.Name : "",
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        IsResolved = c.IsResolved
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminComplaintDto>>(
                    data: complaints,
                    message: $"Retrieved {complaints.Count} complaints",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminComplaintDto>>(
                    message: "Failed to retrieve complaints",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<AdminComplaintDto>> GetComplaintByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                var complaint = await _context.UserGeneralComplaints
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Include(c => c.Category)
                    .Where(c => c.Id == id)
                    .Select(c => new AdminComplaintDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = c.User != null ? c.User.UserName ?? "" : "",
                        UserEmail = c.User != null ? c.User.Email : null,
                        UserPhone = c.User != null ? c.User.PhoneNumber : null,
                        CategoryId = c.CategoryId,
                        CategoryName = c.Category != null ? c.Category.Name : "",
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        IsResolved = c.IsResolved
                    })
                    .FirstOrDefaultAsync(ct);

                if (complaint is null)
                    return new ApiResponse<AdminComplaintDto>("Complaint not found", status: false);

                return new ApiResponse<AdminComplaintDto>(
                    data: complaint,
                    message: "Complaint retrieved successfully",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminComplaintDto>(
                    message: "Failed to retrieve complaint",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<object>> UpdateComplaintStatusAsync(
            int id,
            bool isResolved,
            CancellationToken ct = default)
        {
            try
            {
                var complaint = await _context.UserGeneralComplaints
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

                if (complaint is null)
                    return new ApiResponse<object>("Complaint not found", status: false);

                complaint.IsResolved = isResolved;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { Id = id, IsResolved = isResolved },
                    message: isResolved ? "Complaint marked as resolved" : "Complaint marked as unresolved",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to update complaint status",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }
    }
}
