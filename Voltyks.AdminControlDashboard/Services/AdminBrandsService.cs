using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Brands;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminBrandsService : IAdminBrandsService
    {
        private readonly VoltyksDbContext _context;

        public AdminBrandsService(VoltyksDbContext context)
        {
            _context = context;
        }

        #region Brand CRUD

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

        public async Task<ApiResponse<AdminBrandDto>> GetBrandByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var brand = await _context.Set<Brand>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == id, ct);

                if (brand == null)
                {
                    return new ApiResponse<AdminBrandDto>("Brand not found", false);
                }

                var modelCount = await _context.Set<Model>()
                    .Where(m => m.BrandId == id)
                    .CountAsync(ct);

                var brandDto = new AdminBrandDto
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    LogoUrl = null,
                    TotalModels = modelCount
                };

                return new ApiResponse<AdminBrandDto>(brandDto, "Brand retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminBrandDto>(
                    message: "Failed to retrieve brand",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminBrandDto>> CreateBrandAsync(CreateBrandDto dto, CancellationToken ct = default)
        {
            try
            {
                // Validate name
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return new ApiResponse<AdminBrandDto>("Brand name is required", false);
                }

                // Check if brand already exists
                var existingBrand = await _context.Set<Brand>()
                    .FirstOrDefaultAsync(b => b.Name.ToLower() == dto.Name.ToLower(), ct);

                if (existingBrand != null)
                {
                    return new ApiResponse<AdminBrandDto>("Brand with this name already exists", false);
                }

                // Create new brand
                var newBrand = new Brand
                {
                    Name = dto.Name
                };

                _context.Set<Brand>().Add(newBrand);
                await _context.SaveChangesAsync(ct);

                var brandDto = new AdminBrandDto
                {
                    Id = newBrand.Id,
                    Name = newBrand.Name,
                    LogoUrl = null,
                    TotalModels = 0
                };

                return new ApiResponse<AdminBrandDto>(brandDto, "Brand created successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminBrandDto>(
                    message: "Failed to create brand",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminBrandDto>> UpdateBrandAsync(int id, UpdateBrandDto dto, CancellationToken ct = default)
        {
            try
            {
                // Validate name
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return new ApiResponse<AdminBrandDto>("Brand name is required", false);
                }

                // Find brand
                var brand = await _context.Set<Brand>()
                    .FirstOrDefaultAsync(b => b.Id == id, ct);

                if (brand == null)
                {
                    return new ApiResponse<AdminBrandDto>("Brand not found", false);
                }

                // Check if name already exists for another brand
                var existingBrand = await _context.Set<Brand>()
                    .FirstOrDefaultAsync(b => b.Name.ToLower() == dto.Name.ToLower() && b.Id != id, ct);

                if (existingBrand != null)
                {
                    return new ApiResponse<AdminBrandDto>("Brand with this name already exists", false);
                }

                // Update brand
                brand.Name = dto.Name;
                await _context.SaveChangesAsync(ct);

                // Get model count
                var modelCount = await _context.Set<Model>()
                    .Where(m => m.BrandId == id)
                    .CountAsync(ct);

                var brandDto = new AdminBrandDto
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    LogoUrl = null,
                    TotalModels = modelCount
                };

                return new ApiResponse<AdminBrandDto>(brandDto, "Brand updated successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminBrandDto>(
                    message: "Failed to update brand",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> DeleteBrandAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var brand = await _context.Set<Brand>()
                    .FirstOrDefaultAsync(b => b.Id == id, ct);

                if (brand == null)
                {
                    return new ApiResponse<object>("Brand not found", false);
                }

                // Check if brand has models
                var hasModels = await _context.Set<Model>()
                    .AnyAsync(m => m.BrandId == id, ct);

                if (hasModels)
                {
                    return new ApiResponse<object>(
                        "Cannot delete brand with existing models. Delete models first.",
                        false);
                }

                _context.Set<Brand>().Remove(brand);
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { deletedId = id },
                    message: "Brand deleted successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to delete brand",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        #endregion

        #region Model CRUD

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

        public async Task<ApiResponse<AdminModelDto>> GetModelByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var model = await _context.Set<Model>()
                    .AsNoTracking()
                    .Include(m => m.Brand)
                    .FirstOrDefaultAsync(m => m.Id == id, ct);

                if (model == null)
                {
                    return new ApiResponse<AdminModelDto>("Model not found", false);
                }

                var modelDto = new AdminModelDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    BrandId = model.BrandId,
                    BrandName = model.Brand?.Name ?? "Unknown",
                    Capacity = model.Capacity
                };

                return new ApiResponse<AdminModelDto>(modelDto, "Model retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminModelDto>(
                    message: "Failed to retrieve model",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminModelDto>> CreateModelAsync(CreateModelDto dto, CancellationToken ct = default)
        {
            try
            {
                // Validate name
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return new ApiResponse<AdminModelDto>("Model name is required", false);
                }

                // Validate capacity
                if (dto.Capacity <= 0)
                {
                    return new ApiResponse<AdminModelDto>("Capacity must be greater than zero", false);
                }

                // Check if brand exists
                var brand = await _context.Set<Brand>()
                    .FirstOrDefaultAsync(b => b.Id == dto.BrandId, ct);

                if (brand == null)
                {
                    return new ApiResponse<AdminModelDto>("Brand not found", false);
                }

                // Check if model already exists for this brand
                var existingModel = await _context.Set<Model>()
                    .FirstOrDefaultAsync(m => m.Name.ToLower() == dto.Name.ToLower() && m.BrandId == dto.BrandId, ct);

                if (existingModel != null)
                {
                    return new ApiResponse<AdminModelDto>("Model with this name already exists for this brand", false);
                }

                // Create new model
                var newModel = new Model
                {
                    Name = dto.Name,
                    BrandId = dto.BrandId,
                    Capacity = dto.Capacity
                };

                _context.Set<Model>().Add(newModel);
                await _context.SaveChangesAsync(ct);

                var modelDto = new AdminModelDto
                {
                    Id = newModel.Id,
                    Name = newModel.Name,
                    BrandId = newModel.BrandId,
                    BrandName = brand.Name,
                    Capacity = newModel.Capacity
                };

                return new ApiResponse<AdminModelDto>(modelDto, "Model created successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminModelDto>(
                    message: "Failed to create model",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminModelDto>> UpdateModelAsync(int id, UpdateModelDto dto, CancellationToken ct = default)
        {
            try
            {
                // Validate name
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return new ApiResponse<AdminModelDto>("Model name is required", false);
                }

                // Validate capacity
                if (dto.Capacity <= 0)
                {
                    return new ApiResponse<AdminModelDto>("Capacity must be greater than zero", false);
                }

                // Find model
                var model = await _context.Set<Model>()
                    .FirstOrDefaultAsync(m => m.Id == id, ct);

                if (model == null)
                {
                    return new ApiResponse<AdminModelDto>("Model not found", false);
                }

                // Check if brand exists
                var brand = await _context.Set<Brand>()
                    .FirstOrDefaultAsync(b => b.Id == dto.BrandId, ct);

                if (brand == null)
                {
                    return new ApiResponse<AdminModelDto>("Brand not found", false);
                }

                // Check if name already exists for another model in the same brand
                var existingModel = await _context.Set<Model>()
                    .FirstOrDefaultAsync(m => m.Name.ToLower() == dto.Name.ToLower()
                                           && m.BrandId == dto.BrandId
                                           && m.Id != id, ct);

                if (existingModel != null)
                {
                    return new ApiResponse<AdminModelDto>("Model with this name already exists for this brand", false);
                }

                // Update model
                model.Name = dto.Name;
                model.BrandId = dto.BrandId;
                model.Capacity = dto.Capacity;

                await _context.SaveChangesAsync(ct);

                var modelDto = new AdminModelDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    BrandId = model.BrandId,
                    BrandName = brand.Name,
                    Capacity = model.Capacity
                };

                return new ApiResponse<AdminModelDto>(modelDto, "Model updated successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminModelDto>(
                    message: "Failed to update model",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> DeleteModelAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var model = await _context.Set<Model>()
                    .FirstOrDefaultAsync(m => m.Id == id, ct);

                if (model == null)
                {
                    return new ApiResponse<object>("Model not found", false);
                }

                _context.Set<Model>().Remove(model);
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { deletedId = id },
                    message: "Model deleted successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to delete model",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        #endregion
    }
}
