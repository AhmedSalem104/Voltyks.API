using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Chargers;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminChargersService : IAdminChargersService
    {
        private readonly VoltyksDbContext _context;

        public AdminChargersService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<AdminChargerDto>>> GetChargersAsync(int? userId = null, CancellationToken ct = default)
        {
            try
            {
                var query = _context.Set<Charger>()
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Include(c => c.Protocol)
                    .Include(c => c.Capacity)
                    .Include(c => c.PriceOption)
                    .Include(c => c.Address)
                    .Where(c => !c.IsDeleted);

                if (userId.HasValue)
                {
                    var userIdString = userId.Value.ToString();
                    query = query.Where(c => c.UserId == userIdString);
                }

                var chargers = await query
                    .Select(c => new AdminChargerDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = c.User.FirstName + " " + c.User.LastName,
                        UserEmail = c.User.Email,
                        UserPhone = c.User.PhoneNumber,
                        ProtocolId = c.ProtocolId,
                        ProtocolName = c.Protocol.Name,
                        CapacityId = c.CapacityId,
                        CapacityKw = c.Capacity.kw,
                        PriceOptionId = c.PriceOptionId,
                        PriceValue = c.PriceOption.Value,
                        AddressId = c.AddressId,
                        Area = c.Address.Area,
                        Street = c.Address.Street,
                        BuildingNumber = c.Address.BuildingNumber,
                        Latitude = c.Address.Latitude,
                        Longitude = c.Address.Longitude,
                        IsActive = c.IsActive,
                        IsDeleted = c.IsDeleted,
                        Adaptor = c.Adaptor,
                        AverageRating = c.AverageRating,
                        RatingCount = c.RatingCount,
                        DateAdded = c.DateAdded
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminChargerDto>>(chargers, "Chargers retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminChargerDto>>(
                    message: "Failed to retrieve chargers",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminChargerDto>> GetChargerByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var charger = await _context.Set<Charger>()
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Include(c => c.Protocol)
                    .Include(c => c.Capacity)
                    .Include(c => c.PriceOption)
                    .Include(c => c.Address)
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .Select(c => new AdminChargerDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = c.User.FirstName + " " + c.User.LastName,
                        UserEmail = c.User.Email,
                        UserPhone = c.User.PhoneNumber,
                        ProtocolId = c.ProtocolId,
                        ProtocolName = c.Protocol.Name,
                        CapacityId = c.CapacityId,
                        CapacityKw = c.Capacity.kw,
                        PriceOptionId = c.PriceOptionId,
                        PriceValue = c.PriceOption.Value,
                        AddressId = c.AddressId,
                        Area = c.Address.Area,
                        Street = c.Address.Street,
                        BuildingNumber = c.Address.BuildingNumber,
                        Latitude = c.Address.Latitude,
                        Longitude = c.Address.Longitude,
                        IsActive = c.IsActive,
                        IsDeleted = c.IsDeleted,
                        Adaptor = c.Adaptor,
                        AverageRating = c.AverageRating,
                        RatingCount = c.RatingCount,
                        DateAdded = c.DateAdded
                    })
                    .FirstOrDefaultAsync(ct);

                if (charger == null)
                {
                    return new ApiResponse<AdminChargerDto>("Charger not found", false);
                }

                return new ApiResponse<AdminChargerDto>(charger, "Charger retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminChargerDto>(
                    message: "Failed to retrieve charger",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminChargerDto>> CreateChargerAsync(AdminCreateChargerDto dto, CancellationToken ct = default)
        {
            try
            {
                // Validate user exists
                var user = await _context.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
                if (user == null)
                {
                    return new ApiResponse<AdminChargerDto>("User not found", false);
                }

                // Validate protocol exists
                var protocol = await _context.Set<Protocol>().FirstOrDefaultAsync(p => p.Id == dto.ProtocolId, ct);
                if (protocol == null)
                {
                    return new ApiResponse<AdminChargerDto>("Protocol not found", false);
                }

                // Validate capacity exists
                var capacity = await _context.Set<Capacity>().FirstOrDefaultAsync(c => c.Id == dto.CapacityId, ct);
                if (capacity == null)
                {
                    return new ApiResponse<AdminChargerDto>("Capacity not found", false);
                }

                // Validate price option exists
                var priceOption = await _context.Set<PriceOption>().FirstOrDefaultAsync(p => p.Id == dto.PriceOptionId, ct);
                if (priceOption == null)
                {
                    return new ApiResponse<AdminChargerDto>("Price option not found", false);
                }

                // Validate address fields
                if (string.IsNullOrWhiteSpace(dto.Area) || string.IsNullOrWhiteSpace(dto.Street) || string.IsNullOrWhiteSpace(dto.BuildingNumber))
                {
                    return new ApiResponse<AdminChargerDto>("Address fields are required", false);
                }

                // Create address
                var address = new ChargerAddress
                {
                    Area = dto.Area,
                    Street = dto.Street,
                    BuildingNumber = dto.BuildingNumber,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude
                };

                _context.Set<ChargerAddress>().Add(address);
                await _context.SaveChangesAsync(ct);

                // Create charger
                var charger = new Charger
                {
                    UserId = dto.UserId,
                    ProtocolId = dto.ProtocolId,
                    CapacityId = dto.CapacityId,
                    PriceOptionId = dto.PriceOptionId,
                    AddressId = address.Id,
                    IsActive = dto.IsActive,
                    Adaptor = dto.Adaptor,
                    IsDeleted = false,
                    AverageRating = 0,
                    RatingCount = 0,
                    DateAdded = DateTime.UtcNow
                };

                _context.Set<Charger>().Add(charger);
                await _context.SaveChangesAsync(ct);

                var chargerDto = new AdminChargerDto
                {
                    Id = charger.Id,
                    UserId = charger.UserId,
                    UserName = user.FirstName + " " + user.LastName,
                    UserEmail = user.Email,
                    UserPhone = user.PhoneNumber,
                    ProtocolId = charger.ProtocolId,
                    ProtocolName = protocol.Name,
                    CapacityId = charger.CapacityId,
                    CapacityKw = capacity.kw,
                    PriceOptionId = charger.PriceOptionId,
                    PriceValue = priceOption.Value,
                    AddressId = charger.AddressId,
                    Area = address.Area,
                    Street = address.Street,
                    BuildingNumber = address.BuildingNumber,
                    Latitude = address.Latitude,
                    Longitude = address.Longitude,
                    IsActive = charger.IsActive,
                    IsDeleted = charger.IsDeleted,
                    Adaptor = charger.Adaptor,
                    AverageRating = charger.AverageRating,
                    RatingCount = charger.RatingCount,
                    DateAdded = charger.DateAdded
                };

                return new ApiResponse<AdminChargerDto>(chargerDto, "Charger created successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminChargerDto>(
                    message: "Failed to create charger",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminChargerDto>> UpdateChargerAsync(int id, AdminUpdateChargerDto dto, CancellationToken ct = default)
        {
            try
            {
                var charger = await _context.Set<Charger>()
                    .Include(c => c.Address)
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

                if (charger == null)
                {
                    return new ApiResponse<AdminChargerDto>("Charger not found", false);
                }

                // Validate user exists
                var user = await _context.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
                if (user == null)
                {
                    return new ApiResponse<AdminChargerDto>("User not found", false);
                }

                // Validate protocol exists
                var protocol = await _context.Set<Protocol>().FirstOrDefaultAsync(p => p.Id == dto.ProtocolId, ct);
                if (protocol == null)
                {
                    return new ApiResponse<AdminChargerDto>("Protocol not found", false);
                }

                // Validate capacity exists
                var capacity = await _context.Set<Capacity>().FirstOrDefaultAsync(c => c.Id == dto.CapacityId, ct);
                if (capacity == null)
                {
                    return new ApiResponse<AdminChargerDto>("Capacity not found", false);
                }

                // Validate price option exists
                var priceOption = await _context.Set<PriceOption>().FirstOrDefaultAsync(p => p.Id == dto.PriceOptionId, ct);
                if (priceOption == null)
                {
                    return new ApiResponse<AdminChargerDto>("Price option not found", false);
                }

                // Validate address fields
                if (string.IsNullOrWhiteSpace(dto.Area) || string.IsNullOrWhiteSpace(dto.Street) || string.IsNullOrWhiteSpace(dto.BuildingNumber))
                {
                    return new ApiResponse<AdminChargerDto>("Address fields are required", false);
                }

                // Update charger
                charger.UserId = dto.UserId;
                charger.ProtocolId = dto.ProtocolId;
                charger.CapacityId = dto.CapacityId;
                charger.PriceOptionId = dto.PriceOptionId;
                charger.IsActive = dto.IsActive;
                charger.Adaptor = dto.Adaptor;

                // Update address
                charger.Address.Area = dto.Area;
                charger.Address.Street = dto.Street;
                charger.Address.BuildingNumber = dto.BuildingNumber;
                charger.Address.Latitude = dto.Latitude;
                charger.Address.Longitude = dto.Longitude;

                await _context.SaveChangesAsync(ct);

                var chargerDto = new AdminChargerDto
                {
                    Id = charger.Id,
                    UserId = charger.UserId,
                    UserName = user.FirstName + " " + user.LastName,
                    UserEmail = user.Email,
                    UserPhone = user.PhoneNumber,
                    ProtocolId = charger.ProtocolId,
                    ProtocolName = protocol.Name,
                    CapacityId = charger.CapacityId,
                    CapacityKw = capacity.kw,
                    PriceOptionId = charger.PriceOptionId,
                    PriceValue = priceOption.Value,
                    AddressId = charger.AddressId,
                    Area = charger.Address.Area,
                    Street = charger.Address.Street,
                    BuildingNumber = charger.Address.BuildingNumber,
                    Latitude = charger.Address.Latitude,
                    Longitude = charger.Address.Longitude,
                    IsActive = charger.IsActive,
                    IsDeleted = charger.IsDeleted,
                    Adaptor = charger.Adaptor,
                    AverageRating = charger.AverageRating,
                    RatingCount = charger.RatingCount,
                    DateAdded = charger.DateAdded
                };

                return new ApiResponse<AdminChargerDto>(chargerDto, "Charger updated successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminChargerDto>(
                    message: "Failed to update charger",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> DeleteChargerAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var charger = await _context.Set<Charger>()
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

                if (charger == null)
                {
                    return new ApiResponse<object>("Charger not found", false);
                }

                // Soft delete
                charger.IsDeleted = true;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { deletedId = id },
                    message: "Charger deleted successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to delete charger",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<object>> ToggleChargerStatusAsync(int id, bool isActive, CancellationToken ct = default)
        {
            try
            {
                var charger = await _context.Set<Charger>()
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

                if (charger == null)
                {
                    return new ApiResponse<object>("Charger not found", false);
                }

                charger.IsActive = isActive;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<object>(
                    data: new { id = id, isActive = isActive },
                    message: $"Charger {(isActive ? "activated" : "deactivated")} successfully",
                    status: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(
                    message: "Failed to toggle charger status",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}
