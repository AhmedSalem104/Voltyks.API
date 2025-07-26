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
using Voltyks.Persistence.Entities;

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


        public async Task<ApiResponse<VehicleDto>> CreateVehicleAsync(CreateAndUpdateVehicleDto dto)
        {
            var repo = _unitOfWork.GetRepository<Vehicle, int>();
            var userId = GetCurrentUserId();

            var errors = new List<string>();

            // تحقق من أن المستخدم ليس لديه مركبة بالفعل (عدد المركبات)
            if (await repo.AnyAsync(v => v.UserId == userId && !v.IsDeleted))
                errors.Add(ErrorMessages.UserAlreadyHasVehicle);

            // تحقق من صحة باقي بيانات المركبة باستخدام دالة التحقق الموحدة 
            var dtoErrors = await ValidateVehicleDtoAsync(userId, dto);

            errors.AddRange(dtoErrors);

            if (errors.Any())
            {
                return new ApiResponse<VehicleDto>(
                    message: ErrorMessages.ValidationFailed,
                    status: false,
                    errors: errors
                );
            }

            // إنشاء المركبة بعد نجاح التحقق
            var vehicle = _mapper.Map<Vehicle>(dto);
            vehicle.UserId = userId;

            await repo.AddAsync(vehicle);
            await _unitOfWork.SaveChangesAsync();

            return await GetVehicleDtoById(vehicle.Id, SuccessfulMessage.VehicleCreatedSuccessfully);
        }
        public async Task<ApiResponse<VehicleDto>> UpdateVehicleAsync(int id, CreateAndUpdateVehicleDto dto)
        {
            var repo = _unitOfWork.GetRepository<Vehicle, int>();
            var userId = GetCurrentUserId();

            var vehicle = await repo.GetAsync(id);

            if (vehicle == null || vehicle.UserId != userId)
                return new ApiResponse<VehicleDto>(ErrorMessages.VehicleNotFoundOrNotAuthorized, false);

            // تحقق من صحة بيانات التحديث (تتضمن التحقق من اللوحة)
            var errors = await ValidateVehicleDtoAsync(userId, dto, updatingVehicleId: id);
            if (errors.Any())
                return new ApiResponse<VehicleDto>(ErrorMessages.ValidationFailed, false, errors);

            // تحديث المركبة بعد نجاح التحقق
            _mapper.Map(dto, vehicle);

            repo.Update(vehicle);
            await _unitOfWork.SaveChangesAsync();

            return await GetVehicleDtoById(vehicle.Id, SuccessfulMessage.VehicleUpdatedSuccessfully);
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


            var dtos = vehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                Color = v.Color,
                Plate = v.Plate,
                CreationDate = v.CreationDate,
                Year = v.Year,
                BrandId = v.BrandId,
                BrandName = v.Brand?.Name,
                ModelId = v.ModelId,
                ModelName = v.Model?.Name,
                Capacity = v.Model?.Capacity ?? 0
            }).ToList();



            //var dtos = _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
            return new ApiResponse<IEnumerable<VehicleDto>>(dtos, SuccessfulMessage.VehiclesRetrieved, true);
        }
        public async Task<ApiResponse<bool>> DeleteVehicleAsync(int vehicleId)
        {
            var repo = _unitOfWork.GetRepository<Vehicle, int>();
            var vehicle = await GetVehicleIfAuthorizedAsync(vehicleId);

            if (vehicle == null)
                return new ApiResponse<bool>(ErrorMessages.VehicleNotFoundOrNotAuthorized, false);

            vehicle.IsDeleted = true;
            repo.Update(vehicle);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<bool>(true, SuccessfulMessage.VehicleDeleted, true);
        }


        // ---------- Private Methods ----------
        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new UnauthorizedAccessException(ErrorMessages.UserNotAuthenticated);
        }
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

            var vehicle = vehicleWithIncludes.FirstOrDefault();

            if (vehicle == null)
                return new ApiResponse<VehicleDto>(ErrorMessages.VehicleNotFound, false);

            var dto = _mapper.Map<VehicleDto>(vehicle);
            return new ApiResponse<VehicleDto>(dto, message, true);
        }
        private async Task<List<string>> ValidateVehicleDtoAsync(string userId, CreateAndUpdateVehicleDto dto, int? updatingVehicleId = null)
        {
            var errors = new List<string>();
            var repo = _unitOfWork.GetRepository<Vehicle, int>();

            // تحقق من اللوحة (وجود مركبة أخرى بنفس اللوحة)
            bool plateExists = await repo.AnyAsync(v =>
                v.UserId == userId &&
                v.Plate == dto.Plate &&
                (updatingVehicleId == null || v.Id != updatingVehicleId) &&
                !v.IsDeleted);
            if (plateExists)
                errors.Add(ErrorMessages.PlateAlreadyExists);

            // باقي التحقق مشابه كما عندك
            if (!IsValidEgyptianPlate(dto.Plate))
                errors.Add(ErrorMessages.InvalidPlate);


            if (string.IsNullOrWhiteSpace(dto.Color) || dto.Color.Length > 20)
                errors.Add(ErrorMessages.InvalidColor);

            if (!IsValidVehicleYear(dto.Year))
                errors.Add(ErrorMessages.InvalidYear);


            var brandExists = await _unitOfWork.GetRepository<Brand, int>().AnyAsync(b => b.Id == dto.BrandId);
            if (!brandExists)
                errors.Add(ErrorMessages.InvalidBrand);

            var modelRepo = _unitOfWork.GetRepository<Model, int>();
            var model = await modelRepo.GetFirstOrDefaultAsync(m => m.Id == dto.ModelId);
            if (model == null)
                errors.Add(ErrorMessages.InvalidModel);
            else if (model.BrandId != dto.BrandId)
                errors.Add(ErrorMessages.ModelBrandMismatch);

            return errors;
        }
        private bool IsValidEgyptianPlate(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return false;

            plate = plate.Trim();

            var regex = new System.Text.RegularExpressions.Regex(@"^[0-9\s&\u0621-\u064A]+$");

            return regex.IsMatch(plate);
        }
        private bool IsValidVehicleYear(int year)
        {
            int currentYear = DateTime.Now.Year;
            int minYear = 2000;
            int maxYear = currentYear + 1;

            return year >= minYear && year <= maxYear;
        }
        private async Task<Vehicle?> GetVehicleIfAuthorizedAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<Vehicle, int>();
            var vehicle = await repo.GetAsync(id);
            if (vehicle == null || vehicle.UserId != GetCurrentUserId())
                return null;
            return vehicle;
        }


    }



}
