using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Dtos.Capacity;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminCapacityService : IAdminCapacityService
    {
        private readonly VoltyksDbContext _context;

        public AdminCapacityService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<IEnumerable<AdminCapacityDto>>> GetAllAsync(CancellationToken ct = default)
        {
            var capacities = await _context.Capacities
                .AsNoTracking()
                .OrderBy(c => c.kw)
                .Select(c => new AdminCapacityDto
                {
                    Id = c.Id,
                    KW = c.kw
                })
                .ToListAsync(ct);

            return new ApiResponse<IEnumerable<AdminCapacityDto>>(capacities);
        }

        public async Task<ApiResponse<AdminCapacityDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var capacity = await _context.Capacities
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new AdminCapacityDto
                {
                    Id = c.Id,
                    KW = c.kw
                })
                .FirstOrDefaultAsync(ct);

            if (capacity == null)
                return new ApiResponse<AdminCapacityDto>("Capacity not found", false);

            return new ApiResponse<AdminCapacityDto>(capacity);
        }

        public async Task<ApiResponse<AdminCapacityDto>> CreateAsync(CreateCapacityDto dto, CancellationToken ct = default)
        {
            // Check if KW already exists
            var exists = await _context.Capacities.AnyAsync(c => c.kw == dto.KW, ct);
            if (exists)
                return new ApiResponse<AdminCapacityDto>($"Capacity with {dto.KW} KW already exists", false);

            var capacity = new Capacity
            {
                kw = dto.KW
            };

            _context.Capacities.Add(capacity);
            await _context.SaveChangesAsync(ct);

            var result = new AdminCapacityDto
            {
                Id = capacity.Id,
                KW = capacity.kw
            };

            return new ApiResponse<AdminCapacityDto>(result, "Capacity created successfully", true);
        }

        public async Task<ApiResponse<AdminCapacityDto>> UpdateAsync(int id, UpdateCapacityDto dto, CancellationToken ct = default)
        {
            var capacity = await _context.Capacities.FindAsync(new object[] { id }, ct);

            if (capacity == null)
                return new ApiResponse<AdminCapacityDto>("Capacity not found", false);

            // Check if new KW already exists (excluding current)
            var exists = await _context.Capacities.AnyAsync(c => c.kw == dto.KW && c.Id != id, ct);
            if (exists)
                return new ApiResponse<AdminCapacityDto>($"Capacity with {dto.KW} KW already exists", false);

            capacity.kw = dto.KW;
            await _context.SaveChangesAsync(ct);

            var result = new AdminCapacityDto
            {
                Id = capacity.Id,
                KW = capacity.kw
            };

            return new ApiResponse<AdminCapacityDto>(result, "Capacity updated successfully", true);
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var capacity = await _context.Capacities.FindAsync(new object[] { id }, ct);

            if (capacity == null)
                return new ApiResponse<bool>("Capacity not found", false);

            // Check if capacity is used by any charger
            var isUsed = await _context.Chargers.AnyAsync(c => c.CapacityId == id, ct);
            if (isUsed)
                return new ApiResponse<bool>("Cannot delete: Capacity is used by one or more chargers", false);

            _context.Capacities.Remove(capacity);
            await _context.SaveChangesAsync(ct);

            return new ApiResponse<bool>(true, "Capacity deleted successfully", true);
        }
    }
}
