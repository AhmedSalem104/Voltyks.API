using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.ComplaintCategories;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminComplaintCategoriesService : IAdminComplaintCategoriesService
    {
        private readonly VoltyksDbContext _context;

        public AdminComplaintCategoriesService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<AdminComplaintCategoryDto>>> GetCategoriesAsync(
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.ComplaintCategories.AsNoTracking();

                if (!includeDeleted)
                {
                    query = query.Where(c => !c.IsDeleted);
                }

                var categories = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new AdminComplaintCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        IsDeleted = c.IsDeleted,
                        ComplaintsCount = c.Complaints.Count
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminComplaintCategoryDto>>(
                    data: categories,
                    message: $"Retrieved {categories.Count} categories",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminComplaintCategoryDto>>(
                    message: "Failed to retrieve categories",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<AdminComplaintCategoryDto>> GetCategoryByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                var category = await _context.ComplaintCategories
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new AdminComplaintCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        IsDeleted = c.IsDeleted,
                        ComplaintsCount = c.Complaints.Count
                    })
                    .FirstOrDefaultAsync(ct);

                if (category is null)
                    return new ApiResponse<AdminComplaintCategoryDto>("Category not found", status: false);

                return new ApiResponse<AdminComplaintCategoryDto>(
                    data: category,
                    message: "Category retrieved successfully",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminComplaintCategoryDto>(
                    message: "Failed to retrieve category",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<AdminComplaintCategoryDto>> CreateCategoryAsync(
            CreateComplaintCategoryDto dto,
            CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return new ApiResponse<AdminComplaintCategoryDto>("Category name is required", status: false);

                // Check for duplicate name
                var existingCategory = await _context.ComplaintCategories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower() && !c.IsDeleted, ct);

                if (existingCategory != null)
                    return new ApiResponse<AdminComplaintCategoryDto>("Category with this name already exists", status: false);

                var category = new ComplaintCategory
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.ComplaintCategories.Add(category);
                await _context.SaveChangesAsync(ct);

                var result = new AdminComplaintCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    IsDeleted = category.IsDeleted,
                    ComplaintsCount = 0
                };

                return new ApiResponse<AdminComplaintCategoryDto>(
                    data: result,
                    message: "Category created successfully",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminComplaintCategoryDto>(
                    message: "Failed to create category",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<AdminComplaintCategoryDto>> UpdateCategoryAsync(
            int id,
            UpdateComplaintCategoryDto dto,
            CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return new ApiResponse<AdminComplaintCategoryDto>("Category name is required", status: false);

                var category = await _context.ComplaintCategories
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

                if (category is null)
                    return new ApiResponse<AdminComplaintCategoryDto>("Category not found", status: false);

                // Check for duplicate name (excluding current)
                var existingCategory = await _context.ComplaintCategories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != id && !c.IsDeleted, ct);

                if (existingCategory != null)
                    return new ApiResponse<AdminComplaintCategoryDto>("Category with this name already exists", status: false);

                category.Name = dto.Name;
                category.Description = dto.Description;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                var complaintsCount = await _context.UserGeneralComplaints
                    .CountAsync(c => c.CategoryId == id, ct);

                var result = new AdminComplaintCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    IsDeleted = category.IsDeleted,
                    ComplaintsCount = complaintsCount
                };

                return new ApiResponse<AdminComplaintCategoryDto>(
                    data: result,
                    message: "Category updated successfully",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminComplaintCategoryDto>(
                    message: "Failed to update category",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<object>> DeleteCategoryAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var category = await _context.ComplaintCategories
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

                if (category is null)
                    return new ApiResponse<object>("Category not found", status: false);

                if (category.IsDeleted)
                    return new ApiResponse<object>("Category is already deleted", status: false);

                category.IsDeleted = true;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { Id = id },
                    message: "Category deleted successfully",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to delete category",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<object>> RestoreCategoryAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var category = await _context.ComplaintCategories
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

                if (category is null)
                    return new ApiResponse<object>("Category not found", status: false);

                if (!category.IsDeleted)
                    return new ApiResponse<object>("Category is not deleted", status: false);

                category.IsDeleted = false;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { Id = id },
                    message: "Category restored successfully",
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to restore category",
                    status: false,
                    errors: new List<string> { ex.Message }
                );
            }
        }
    }
}
