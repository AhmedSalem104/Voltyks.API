using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Vehicles;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminVehiclesService : IAdminVehiclesService
    {
        private readonly VoltyksDbContext _context;

        public AdminVehiclesService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<AdminVehicleDto>>> GetVehiclesAsync(string? userId = null, CancellationToken ct = default)
        {
            try
            {
                var query = _context.Set<Vehicle>()
                    .AsNoTracking()
                    .Include(v => v.Brand)
                    .Include(v => v.Model)
                    .Include(v => v.User)
                    .Where(v => !v.IsDeleted);

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    query = query.Where(v => v.UserId == userId);
                }

                var vehicles = await query
                    .Select(v => new AdminVehicleDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Plate = v.Plate,
                        CreationDate = v.CreationDate,
                        Year = v.Year,
                        IsDeleted = v.IsDeleted,
                        BrandId = v.BrandId,
                        BrandName = v.Brand.Name,
                        ModelId = v.ModelId,
                        ModelName = v.Model.Name,
                        ModelCapacity = v.Model.Capacity,
                        UserId = v.UserId,
                        UserName = v.User.FirstName + " " + v.User.LastName,
                        UserEmail = v.User.Email,
                        UserPhone = v.User.PhoneNumber
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminVehicleDto>>(vehicles, "Vehicles retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminVehicleDto>>(
                    message: "Failed to retrieve vehicles",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminVehicleDto>> GetVehicleByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var vehicle = await _context.Set<Vehicle>()
                    .AsNoTracking()
                    .Include(v => v.Brand)
                    .Include(v => v.Model)
                    .Include(v => v.User)
                    .Where(v => v.Id == id && !v.IsDeleted)
                    .Select(v => new AdminVehicleDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Plate = v.Plate,
                        CreationDate = v.CreationDate,
                        Year = v.Year,
                        IsDeleted = v.IsDeleted,
                        BrandId = v.BrandId,
                        BrandName = v.Brand.Name,
                        ModelId = v.ModelId,
                        ModelName = v.Model.Name,
                        ModelCapacity = v.Model.Capacity,
                        UserId = v.UserId,
                        UserName = v.User.FirstName + " " + v.User.LastName,
                        UserEmail = v.User.Email,
                        UserPhone = v.User.PhoneNumber
                    })
                    .FirstOrDefaultAsync(ct);

                if (vehicle == null)
                {
                    return new ApiResponse<AdminVehicleDto>("Vehicle not found", false);
                }

                return new ApiResponse<AdminVehicleDto>(vehicle, "Vehicle retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminVehicleDto>(
                    message: "Failed to retrieve vehicle",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminVehicleDto>> CreateVehicleAsync(CreateVehicleDto dto, CancellationToken ct = default)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(dto.Color))
                {
                    return new ApiResponse<AdminVehicleDto>("Color is required", false);
                }

                if (string.IsNullOrWhiteSpace(dto.Plate))
                {
                    return new ApiResponse<AdminVehicleDto>("Plate is required", false);
                }

                if (dto.Year < 1900 || dto.Year > DateTime.Now.Year + 1)
                {
                    return new ApiResponse<AdminVehicleDto>("Invalid year", false);
                }

                // Validate user exists
                var user = await _context.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
                if (user == null)
                {
                    return new ApiResponse<AdminVehicleDto>("User not found", false);
                }

                // Validate brand exists
                var brand = await _context.Set<Brand>().FirstOrDefaultAsync(b => b.Id == dto.BrandId, ct);
                if (brand == null)
                {
                    return new ApiResponse<AdminVehicleDto>("Brand not found", false);
                }

                // Validate model exists and belongs to brand
                var model = await _context.Set<Model>().FirstOrDefaultAsync(m => m.Id == dto.ModelId && m.BrandId == dto.BrandId, ct);
                if (model == null)
                {
                    return new ApiResponse<AdminVehicleDto>("Model not found or does not belong to specified brand", false);
                }

                // Check if plate already exists
                var existingVehicle = await _context.Set<Vehicle>()
                    .FirstOrDefaultAsync(v => v.Plate.ToLower() == dto.Plate.ToLower() && !v.IsDeleted, ct);
                if (existingVehicle != null)
                {
                    return new ApiResponse<AdminVehicleDto>("Vehicle with this plate already exists", false);
                }

                // Create vehicle
                var vehicle = new Vehicle
                {
                    Color = dto.Color,
                    Plate = dto.Plate,
                    Year = dto.Year,
                    BrandId = dto.BrandId,
                    ModelId = dto.ModelId,
                    UserId = dto.UserId,
                    CreationDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.Set<Vehicle>().Add(vehicle);
                await _context.SaveChangesAsync(ct);

                var vehicleDto = new AdminVehicleDto
                {
                    Id = vehicle.Id,
                    Color = vehicle.Color,
                    Plate = vehicle.Plate,
                    CreationDate = vehicle.CreationDate,
                    Year = vehicle.Year,
                    IsDeleted = vehicle.IsDeleted,
                    BrandId = vehicle.BrandId,
                    BrandName = brand.Name,
                    ModelId = vehicle.ModelId,
                    ModelName = model.Name,
                    ModelCapacity = model.Capacity,
                    UserId = vehicle.UserId,
                    UserName = user.FirstName + " " + user.LastName,
                    UserEmail = user.Email,
                    UserPhone = user.PhoneNumber
                };

                return new ApiResponse<AdminVehicleDto>(vehicleDto, "Vehicle created successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminVehicleDto>(
                    message: "Failed to create vehicle",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminVehicleDto>> UpdateVehicleAsync(int id, UpdateVehicleDto dto, CancellationToken ct = default)
        {
            try
            {
                var vehicle = await _context.Set<Vehicle>()
                    .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);

                if (vehicle == null)
                {
                    return new ApiResponse<AdminVehicleDto>("Vehicle not found", false);
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(dto.Color))
                {
                    return new ApiResponse<AdminVehicleDto>("Color is required", false);
                }

                if (string.IsNullOrWhiteSpace(dto.Plate))
                {
                    return new ApiResponse<AdminVehicleDto>("Plate is required", false);
                }

                if (dto.Year < 1900 || dto.Year > DateTime.Now.Year + 1)
                {
                    return new ApiResponse<AdminVehicleDto>("Invalid year", false);
                }

                // Validate user exists
                var user = await _context.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
                if (user == null)
                {
                    return new ApiResponse<AdminVehicleDto>("User not found", false);
                }

                // Validate brand exists
                var brand = await _context.Set<Brand>().FirstOrDefaultAsync(b => b.Id == dto.BrandId, ct);
                if (brand == null)
                {
                    return new ApiResponse<AdminVehicleDto>("Brand not found", false);
                }

                // Validate model exists and belongs to brand
                var model = await _context.Set<Model>().FirstOrDefaultAsync(m => m.Id == dto.ModelId && m.BrandId == dto.BrandId, ct);
                if (model == null)
                {
                    return new ApiResponse<AdminVehicleDto>("Model not found or does not belong to specified brand", false);
                }

                // Check if plate already exists for another vehicle
                var existingVehicle = await _context.Set<Vehicle>()
                    .FirstOrDefaultAsync(v => v.Plate.ToLower() == dto.Plate.ToLower() && v.Id != id && !v.IsDeleted, ct);
                if (existingVehicle != null)
                {
                    return new ApiResponse<AdminVehicleDto>("Vehicle with this plate already exists", false);
                }

                // Update vehicle
                vehicle.Color = dto.Color;
                vehicle.Plate = dto.Plate;
                vehicle.Year = dto.Year;
                vehicle.BrandId = dto.BrandId;
                vehicle.ModelId = dto.ModelId;
                vehicle.UserId = dto.UserId;

                await _context.SaveChangesAsync(ct);

                var vehicleDto = new AdminVehicleDto
                {
                    Id = vehicle.Id,
                    Color = vehicle.Color,
                    Plate = vehicle.Plate,
                    CreationDate = vehicle.CreationDate,
                    Year = vehicle.Year,
                    IsDeleted = vehicle.IsDeleted,
                    BrandId = vehicle.BrandId,
                    BrandName = brand.Name,
                    ModelId = vehicle.ModelId,
                    ModelName = model.Name,
                    ModelCapacity = model.Capacity,
                    UserId = vehicle.UserId,
                    UserName = user.FirstName + " " + user.LastName,
                    UserEmail = user.Email,
                    UserPhone = user.PhoneNumber
                };

                return new ApiResponse<AdminVehicleDto>(vehicleDto, "Vehicle updated successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminVehicleDto>(
                    message: "Failed to update vehicle",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> DeleteVehicleAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var vehicle = await _context.Set<Vehicle>()
                    .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);

                if (vehicle == null)
                {
                    return new ApiResponse<object>("Vehicle not found", false);
                }

                // Soft delete
                vehicle.IsDeleted = true;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { deletedId = id },
                    message: "Vehicle deleted successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to delete vehicle",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}
