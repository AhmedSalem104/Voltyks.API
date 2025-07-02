using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Main;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Voltyks.Persistence.Entities;
using System.Threading.Channels;

namespace Voltyks.Application.Interfaces.ChargerStation
{
    public class ChargerService : IChargerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContext;

        public ChargerService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContext = httpContext;
        }
        public async Task<ApiResponse<IEnumerable<CapacityDto>>> GetAllCapacitiesAsync()
        {
            var capacities = await _unitOfWork.GetRepository<Capacity, int>().GetAllAsync();
            var data = _mapper.Map<IEnumerable<CapacityDto>>(capacities);

            return new ApiResponse<IEnumerable<CapacityDto>>(data);
        }
        public async Task<ApiResponse<IEnumerable<ProtocolDto>>> GetAllProtocolsAsync()
        {
            var protocols = await _unitOfWork.GetRepository<Protocol, int>().GetAllAsync();
            var data = _mapper.Map<IEnumerable<ProtocolDto>>(protocols);
            return new ApiResponse<IEnumerable<ProtocolDto>>(data);
        }
        public async Task<ApiResponse<IEnumerable<PriceByCapacityDto>>> GetPriceListBasedOnCapacityAsync()
        {
            var capacities = await _unitOfWork.GetRepository<Capacity, int>().GetAllAsync();
            var prices = await _unitOfWork.GetRepository<PriceOption, int>().GetAllAsync();

            var result = capacities.Select(c => new PriceByCapacityDto
            {
                Capacity = c.KW,
                AvailablePrices = prices.Select(p => p.Value).ToList()
            });

            return new ApiResponse<IEnumerable<PriceByCapacityDto>>(result);
        }
        public async Task<ApiResponse<string>> AddChargerAsync(AddChargerDto dto)
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<string>(ErrorMessages.UnauthorizedAccess, false);

            var userId = userIdClaim.Value;

            // إنشاء العنوان
            var address = new ChargerAddress
            {
                Area = dto.Area,
                Street = dto.Street,
                BuildingNumber = dto.BuildingNumber,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };
            await _unitOfWork.GetRepository<ChargerAddress, int>().AddAsync(address);
            await _unitOfWork.SaveChangesAsync();

            // استخدام AutoMapper لبناء الكائن
            var charger = _mapper.Map<Charger>(dto);

            // نضيف القيم غير الموجودة في DTO
            charger.UserId = userId;
            charger.AddressId = address.Id;
            charger.DateAdded = DateTime.UtcNow;
            charger.IsDeleted = false;

            await _unitOfWork.GetRepository<Charger, int>().AddAsync(charger);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<string>(message: "Charger added successfully");
        }
        public async Task<ApiResponse<IEnumerable<ChargerDto>>> GetChargersForCurrentUserAsync()
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<IEnumerable<ChargerDto>>("Unauthorized", false);

            var userId = userIdClaim.Value;

            var chargers = await _unitOfWork.GetRepository<Charger, int>()
                                 .GetAllWithIncludeAsync(
                                     c => c.UserId == userId && !c.IsDeleted,
                                     false,
                                     c => c.Protocol,
                                     c => c.Capacity,
                                     c => c.PriceOption,
                                     c => c.Address
                                 );


            var result = _mapper.Map<IEnumerable<ChargerDto>>(chargers);

            return new ApiResponse<IEnumerable<ChargerDto>>(result);
        }

        public async Task<ApiResponse<string>> ToggleChargerStatusAsync(int chargerId)
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<string>("Unauthorized", false);

            var userId = userIdClaim.Value;

            var charger = await _unitOfWork.GetRepository<Charger, int>().GetAsync(chargerId);

            if (charger == null || charger.IsDeleted)
                return new ApiResponse<string>("Charger not found", false);

            if (charger.UserId != userId)
                return new ApiResponse<string>("You are not authorized to modify this charger", true);

            // عكس الحالة
            charger.IsActive = !charger.IsActive;

            _unitOfWork.GetRepository<Charger, int>().Update(charger);
            await _unitOfWork.SaveChangesAsync();

            string newStatus = charger.IsActive ? "Active" : "Not Active";
            string dataValue = charger.IsActive ? "true" : "false"; 

            return new ApiResponse<string>(
                data: dataValue,
                message: $"Charger status changed to: {newStatus}",
                status: true
            );
        }





        public async Task<ApiResponse<string>> UpdateChargerAsync(UpdateChargerDto dto)
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<string>("Unauthorized", false);

            var userId = userIdClaim.Value;

            var charger = await _unitOfWork.GetRepository<Charger, int>().GetAllWithIncludeAsync(
                c => c.Id == dto.ChargerId && !c.IsDeleted,
                false,
                c => c.Address
            );

            var chargerEntity = charger.FirstOrDefault();

            if (chargerEntity == null)
                return new ApiResponse<string>("Charger not found", false);

            if (chargerEntity.UserId != userId)
                return new ApiResponse<string>("Unauthorized access to charger", false);

            // تحديث البيانات
            chargerEntity.ProtocolId = dto.ProtocolId;
            chargerEntity.CapacityId = dto.CapacityId;
            chargerEntity.PriceOptionId = dto.PriceOptionId;
            chargerEntity.IsActive = dto.IsActive;

            // تحديث العنوان المرتبط
            chargerEntity.Address.Area = dto.Area;
            chargerEntity.Address.Street = dto.Street;
            chargerEntity.Address.BuildingNumber = dto.BuildingNumber;
            chargerEntity.Address.Latitude = dto.Latitude;
            chargerEntity.Address.Longitude = dto.Longitude;

            _unitOfWork.GetRepository<Charger, int>().Update(chargerEntity);
            _unitOfWork.GetRepository<ChargerAddress, int>().Update(chargerEntity.Address);
            await _unitOfWork.SaveChangesAsync();
            return new ApiResponse<string>(message: "Charger updated successfully");

        }

        public async Task<ApiResponse<string>> DeleteChargerAsync(int chargerId)
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<string>("Unauthorized", false);

            var userId = userIdClaim.Value;

            var charger = await _unitOfWork.GetRepository<Charger, int>().GetAsync(chargerId);

            if (charger == null || charger.IsDeleted)
                return new ApiResponse<string>("Charger not found or already deleted", false);

            if (charger.UserId != userId)
                return new ApiResponse<string>("You are not authorized to delete this charger", false);

            charger.IsDeleted = true;
            _unitOfWork.GetRepository<Charger, int>().Update(charger);
            await _unitOfWork.SaveChangesAsync();
            return new ApiResponse<string>(message: "Charger deleted successfully");

        }




    }

}
