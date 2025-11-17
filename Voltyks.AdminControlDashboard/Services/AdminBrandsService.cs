using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Brands;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminBrandsService : IAdminBrandsService
    {
        private readonly VoltyksDbContext _context;

        public AdminBrandsService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<AdminBrandDto>>> GetBrandsAsync(CancellationToken ct = default)
        {
            try
            {
                // Get brands with model count from separate query
                var brandsWithCounts = await (from brand in _context.Set<Voltyks.Persistence.Entities.Main.Brand>()
                                             join model in _context.Set<Voltyks.Persistence.Entities.Main.Model>()
                                             on brand.Id equals model.BrandId into models
                                             select new AdminBrandDto
                                             {
                                                 Id = brand.Id,
                                                 Name = brand.Name,
                                                 LogoUrl = null, // Brand doesn't have LogoUrl property
                                                 TotalModels = models.Count()
                                             })
                                            .AsNoTracking()
                                            .ToListAsync(ct);

                var brands = brandsWithCounts;

                return new ApiResponse<List<AdminBrandDto>>(brands, "Brands retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminBrandDto>>(
                    message: "Failed to retrieve brands",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<AdminModelDto>>> GetModelsAsync(int? brandId = null, CancellationToken ct = default)
        {
            try
            {
                IQueryable<Voltyks.Persistence.Entities.Main.Model> query = _context.Set<Voltyks.Persistence.Entities.Main.Model>()
                    .AsNoTracking()
                    .Include(m => m.Brand);

                if (brandId.HasValue)
                {
                    query = query.Where(m => m.BrandId == brandId.Value);
                }

                var models = await query
                    .Select(m => new AdminModelDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        BrandId = m.BrandId,
                        BrandName = m.Brand.Name,
                        Capacity = m.Capacity
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminModelDto>>(models, "Models retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminModelDto>>(
                    message: "Failed to retrieve models",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}
