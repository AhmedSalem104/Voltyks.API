using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.VehicleAdditionRequest;
using Voltyks.Core.Constants;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.VehicleAdditionRequests;
using Voltyks.Persistence.Data;
using VehicleAdditionRequestEntity = Voltyks.Persistence.Entities.Main.VehicleAdditionRequest;

namespace Voltyks.Application.Services.VehicleAdditionRequest
{
    public class VehicleAdditionRequestService : IVehicleAdditionRequestService
    {
        private readonly VoltyksDbContext _context;

        public VehicleAdditionRequestService(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<UserVehicleAdditionRequestDto>> CreateAsync(
            string userId, CreateVehicleAdditionRequestDto dto, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return new ApiResponse<UserVehicleAdditionRequestDto>("Unauthorized", status: false);

                var brandName = dto.BrandName?.Trim() ?? "";
                var modelName = dto.ModelName?.Trim() ?? "";
                var capacity = dto.Capacity?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(brandName) || string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(capacity))
                    return new ApiResponse<UserVehicleAdditionRequestDto>("BrandName, ModelName, and Capacity are required", status: false);

                // Check if Brand+Model already exist in the system
                var exists = await _context.Models
                    .AnyAsync(m =>
                        m.Name.ToLower() == modelName.ToLower() &&
                        m.Brand != null &&
                        m.Brand.Name.ToLower() == brandName.ToLower(),
                        ct);

                if (exists)
                    return new ApiResponse<UserVehicleAdditionRequestDto>(
                        "This vehicle already exists in our system. Please check the vehicles list again.",
                        status: false);

                // Prevent duplicate pending request from same user
                var duplicatePending = await _context.VehicleAdditionRequests
                    .AnyAsync(r =>
                        r.UserId == userId &&
                        r.Status == VehicleAdditionRequestStatuses.Pending &&
                        r.BrandName.ToLower() == brandName.ToLower() &&
                        r.ModelName.ToLower() == modelName.ToLower(),
                        ct);

                if (duplicatePending)
                    return new ApiResponse<UserVehicleAdditionRequestDto>(
                        "You already have a pending request for this vehicle.",
                        status: false);

                var request = new VehicleAdditionRequestEntity
                {
                    UserId = userId,
                    BrandName = brandName,
                    ModelName = modelName,
                    Capacity = capacity,
                    Status = VehicleAdditionRequestStatuses.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.VehicleAdditionRequests.Add(request);
                await _context.SaveChangesAsync(ct);

                var result = new UserVehicleAdditionRequestDto
                {
                    Id = request.Id,
                    BrandName = request.BrandName,
                    ModelName = request.ModelName,
                    Capacity = request.Capacity,
                    Status = request.Status,
                    CreatedAt = request.CreatedAt,
                    UpdatedAt = request.UpdatedAt
                };

                return new ApiResponse<UserVehicleAdditionRequestDto>(result, "Request submitted successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserVehicleAdditionRequestDto>(
                    message: "Failed to submit request",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<UserVehicleAdditionRequestDto>>> GetMyRequestsAsync(
            string userId, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return new ApiResponse<List<UserVehicleAdditionRequestDto>>("Unauthorized", status: false);

                var items = await _context.VehicleAdditionRequests
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new UserVehicleAdditionRequestDto
                    {
                        Id = r.Id,
                        BrandName = r.BrandName,
                        ModelName = r.ModelName,
                        Capacity = r.Capacity,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<UserVehicleAdditionRequestDto>>(items, $"Retrieved {items.Count} requests", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<UserVehicleAdditionRequestDto>>(
                    message: "Failed to retrieve requests",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }
    }
}
