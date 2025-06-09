using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Voltyks.Core.DTOs.VehicleDTOs;
using Voltyks.Core.DTOs;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Main;
using System.Linq.Expressions;
using Voltyks.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Voltyks.Application.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VehicleService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        // ✅ استخراج UserId من التوكن
        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new UnauthorizedAccessException("User not authenticated.");
        }

        public async Task<ApiResponse<VehicleDto>> CreateVehicleAsync(CreateVehicleDto dto)
        {
            var repo = _unitOfWork.GetRepository<Vehicle, int>();
            var vehicle = _mapper.Map<Vehicle>(dto);
            vehicle.UserId = GetCurrentUserId(); // ربط بالمستخدم الحالي

            await repo.AddAsync(vehicle);
            await _unitOfWork.SaveChangesAsync();

            return await GetVehicleDtoById(vehicle.Id, "Vehicle created successfully");
        }

        public async Task<ApiResponse<VehicleDto>> UpdateVehicleAsync(int id, UpdateVehicleDto dto)
        {
            var repo = _unitOfWork.GetRepository<Vehicle, int>();
            var vehicle = await repo.GetAsync(id);

            if (vehicle == null || vehicle.UserId != GetCurrentUserId())
                return new ApiResponse<VehicleDto>("Vehicle not found or not authorized", false);

            _mapper.Map(dto, vehicle);
            repo.Update(vehicle);
            await _unitOfWork.SaveChangesAsync();

            return await GetVehicleDtoById(vehicle.Id, "Vehicle updated successfully");
        }

        public async Task<ApiResponse<IEnumerable<VehicleDto>>> GetVehiclesByUserIdAsync()
        {
            var currentUserId = GetCurrentUserId();

            var vehicles = await _unitOfWork
                .GetRepository<Vehicle, int>()
                .GetAllWithIncludeAsync(
                    filter: v => v.UserId == currentUserId && !v.IsDeleted,
                    includes: new Expression<Func<Vehicle, object>>[]
                    {
                    v => v.Brand,
                    v => v.Model
                    });

            var dtos = _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
            return new ApiResponse<IEnumerable<VehicleDto>>(dtos, "Vehicles retrieved", true);
        }

        public async Task<ApiResponse<bool>> DeleteVehicleAsync(int vehicleId)
        {
            var repo = _unitOfWork.GetRepository<Vehicle, int>();
            var vehicle = await repo.GetAsync(vehicleId);

            if (vehicle == null || vehicle.UserId != GetCurrentUserId())
                return new ApiResponse<bool>("Vehicle not found or not authorized", false);

            vehicle.IsDeleted = true;
            repo.Update(vehicle);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<bool>(true, "Vehicle deleted", true);
        }

        // 🔹 Helper method
        private async Task<ApiResponse<VehicleDto>> GetVehicleDtoById(int id, string message)
        {
            var repo = _unitOfWork.GetRepository<Vehicle, int>();

            var vehicleWithIncludes = await repo.GetAllWithIncludeAsync(
                filter: v => v.Id == id,
                includes: new Expression<Func<Vehicle, object>>[]
                {
                v => v.Brand,
                v => v.Model
                });

            var dto = _mapper.Map<VehicleDto>(vehicleWithIncludes.FirstOrDefault());
            return new ApiResponse<VehicleDto>(dto, message, true);
        }
    }


 
}
