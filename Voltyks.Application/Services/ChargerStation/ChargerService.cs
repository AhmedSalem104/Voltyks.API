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
using static System.Net.Mime.MediaTypeNames;
using Voltyks.Application.Interfaces.AppSettings;

namespace Voltyks.Application.Interfaces.ChargerStation
{
    public class ChargerService : IChargerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IAppSettingsService _appSettingsService;

        public ChargerService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContext, IAppSettingsService appSettingsService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContext = httpContext;
            _appSettingsService = appSettingsService;
        }
        public async Task<ApiResponse<IEnumerable<CapacityDto>>> GetAllCapacitiesAsync()
        {
            var capacities = await _unitOfWork.GetRepository<Capacity, int>().GetAllAsync();
            var sortedCapacities = capacities.OrderBy(c => c.kw);
            var data = _mapper.Map<IEnumerable<CapacityDto>>(sortedCapacities);

            return new ApiResponse<IEnumerable<CapacityDto>>(data);
        }
        public async Task<ApiResponse<IEnumerable<ProtocolDto>>> GetAllProtocolsAsync()
        {
            var protocols = await _unitOfWork.GetRepository<Protocol, int>().GetAllAsync();
            var data = _mapper.Map<IEnumerable<ProtocolDto>>(protocols);
            return new ApiResponse<IEnumerable<ProtocolDto>>(data);
        }
        public async Task<ApiResponse<IEnumerable<PriceByCapacityDto>>> GetPriceListAsync()
        {
            var Prices = await _unitOfWork.GetRepository<PriceOption, int>().GetAllAsync();
            var data = _mapper.Map<IEnumerable<PriceByCapacityDto>>(Prices);
            return new ApiResponse<IEnumerable<PriceByCapacityDto>>(data);
        } 
        public async Task<ApiResponse<string>> AddChargerAsync(AddChargerDto dto)
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<string>(ErrorMessages.UnauthorizedAccess, false);

            var userId = userIdClaim.Value;

            if (!await ProtocolExistsAsync(dto.ProtocolId))
                return new ApiResponse<string>($"ProtocolId {dto.ProtocolId} does not exist.", false);

            if (!await CapacityExistsAsync(dto.CapacityId))
                return new ApiResponse<string>($"CapacityId {dto.CapacityId} does not exist.", false);

            if (!await PriceOptionExistsAsync(dto.PriceOptionId))
                return new ApiResponse<string>($"PriceOptionId {dto.PriceOptionId} does not exist.", false);

            var address = await CreateAddressAsync(dto);

            await AddChargerRecordAsync(dto, userId, address.Id);

            return new ApiResponse<string>(SuccessfulMessage.ChargerAddedSuccessfully, true);
        }
        public async Task<ApiResponse<string>> UpdateChargerAsync(UpdateChargerDto dto, int chargerId)
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<string>(ErrorMessages.UnauthorizedAccess, false);

            var userId = userIdClaim.Value;

            // التحقق من وجود القيم المرجعية
            if (!await ProtocolExistsAsync(dto.ProtocolId))
                return new ApiResponse<string>($"ProtocolId {dto.ProtocolId} does not exist.", false);

            if (!await CapacityExistsAsync(dto.CapacityId))
                return new ApiResponse<string>($"CapacityId {dto.CapacityId} does not exist.", false);

            if (!await PriceOptionExistsAsync(dto.PriceOptionId))
                return new ApiResponse<string>($"PriceOptionId {dto.PriceOptionId} does not exist.", false);

            // جلب الشاحن مع العنوان
            var charger = await GetChargerWithAddressAsync(chargerId, userId);
            if (charger == null)
                return new ApiResponse<string>(ErrorMessages.ChargerNotFound, false);

            // تحديث البيانات
            UpdateChargerAndAddress(charger, dto);

            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<string>(SuccessfulMessage.ChargerUpdatedSuccessfully, true);
        }     
        public async Task<ApiResponse<string>> DeleteChargerAsync(int chargerId)
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<string>(ErrorMessages.UnauthorizedAccess, false);

            var userId = userIdClaim.Value;

            var charger = await _unitOfWork.GetRepository<Charger, int>().GetAsync(chargerId);

            if (charger == null || charger.IsDeleted)
                return new ApiResponse<string>(ErrorMessages.ChargerNotFoundOrAlreadyDeleted, false);

            if (charger.UserId != userId)
                return new ApiResponse<string>(ErrorMessages.YouAreNotAuthorizedToDeleteThisCharger, false);

            charger.IsDeleted = true;
            _unitOfWork.GetRepository<Charger, int>().Update(charger);
            await _unitOfWork.SaveChangesAsync();
            return new ApiResponse<string>(message: SuccessfulMessage.ChargerDeletedSuccessfully,true);

        }
        public async Task<ApiResponse<bool>> ToggleChargerStatusAsync(int chargerId)
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<bool>(ErrorMessages.UnauthorizedAccess, false);

            var userId = userIdClaim.Value;

            var charger = await _unitOfWork.GetRepository<Charger, int>().GetAsync(chargerId);

            if (charger == null || charger.IsDeleted)
                return new ApiResponse<bool>(ErrorMessages.ChargerNotFound, false);

            if (charger.UserId != userId)
                return new ApiResponse<bool>(ErrorMessages.YouAreNotAuthorizedToModifyThisCharger, true);

            // التحقق من وضع الشحن - منع التفعيل لو الوضع مُعطّل
            var chargingModeEnabled = await _appSettingsService.IsChargingModeEnabledAsync();
            if (!chargingModeEnabled && !charger.IsActive)
            {
                return new ApiResponse<bool>(
                    data: false,
                    message: "Cannot activate charger. Charging mode is not enabled yet. Please wait for admin activation.",
                    status: false
                );
            }

            // عكس الحالة
            charger.IsActive = !charger.IsActive;

            _unitOfWork.GetRepository<Charger, int>().Update(charger);
            await _unitOfWork.SaveChangesAsync();

            string newStatus = charger.IsActive ? "Active" : "Not Active";
            bool dataValue = charger.IsActive ? true : false;

            return new ApiResponse<bool>(
                data: dataValue,
                message: $"Charger status changed to: {newStatus}",
                status: true
            );
        }
        public async Task<ApiResponse<IEnumerable<ChargerDto>>> GetChargersForCurrentUserAsync()
        {
            var userIdClaim = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<IEnumerable<ChargerDto>>(ErrorMessages.UnauthorizedAccess, false);

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
        public async Task<ApiResponse<List<NearChargerDto>>> GetNearChargersAsync(NearChargerSearchDto searchDto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return new ApiResponse<List<NearChargerDto>>(ErrorMessages.UnauthorizedAccess, false);

            var chargers = await GetChargersFromDbAsync(searchDto, currentUserId);
            var filteredChargers = FilterChargersByDistance(chargers, searchDto);
            //var paginatedChargers = ApplyPagination(filteredChargers, searchDto);
            var result = MapToDto(filteredChargers);

            return new ApiResponse<List<NearChargerDto>>(result, "Success", true);
        }
        public async Task<ApiResponse<ChargerDetailsDto>> GetChargerByIdAsync(ChargerDetailsRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return new ApiResponse<ChargerDetailsDto>(ErrorMessages.UnauthorizedAccess, false);

            var charger = await GetChargerById(request.ChargerId);
            if (charger == null)
                return new ApiResponse<ChargerDetailsDto>(ErrorMessages.ChargerNotFound, false);



            var dto = new ChargerDetailsDto
            {
                FullName = new StringBuilder()
                    .Append(charger.User.FirstName)
                    .Append(" ") 
                    .Append(charger.User.LastName)
                    .ToString(),
                PhoneNumber = charger.User.PhoneNumber,
                Rating = charger.User.Rating,
                RatingCount = charger.User.RatingCount,
                Area = charger.Address?.Area,
                Street = charger.Address?.Street,
                BuildingNumber = charger.Address.BuildingNumber,
                Latitude = charger.Address.Latitude,
                Longitude = charger.Address.Longitude,
                Protocol = charger.Protocol?.Name,
                Capacity = charger.Capacity != null ? new CapacityDto { KW = charger.Capacity.kw } : null,
                PricePerHour = charger.PriceOption != null ? charger.PriceOption.Value : 0m,
        

                AdapterAvailability = charger.Adaptor == true ? "Available" : "Not Available",
                KwNeeded = request.KwNeed,
                TimeNeeded = charger.Capacity?.kw > 0
                    ? Math.Round((request.KwNeed / charger.Capacity.kw) * 60, 0)
                    : 0
            };


                  CalculateDistanceAndArrival(request.UserLat, request.UserLon, charger, dto);
                   SetEstimatedPrice(request.KwNeed, charger, dto);

            return new ApiResponse<ChargerDetailsDto>(dto, "Success", true);
        }



        private async Task<List<Charger>> GetChargersFromDbAsync(NearChargerSearchDto searchDto, string currentUserId)
        {
            // فصل منطق عرض الشاحن عن Adapter فقط
            // الـ Adapter تكون معلومة إضافية وليست شرط للظهور
            // لكن IsAvailable و IsActive لازم يكونوا true
            return (await _unitOfWork.GetRepository<Charger, int>().GetAllWithIncludeAsync(
               c => !c.IsDeleted && c.IsActive &&
                    c.ProtocolId == searchDto.ProtocolId &&
                    c.User.IsAvailable == true &&
                    c.User.IsBanned == false &&
                    c.User.Id != currentUserId,
               false,
               c => c.Capacity,
               c => c.PriceOption,
               c => c.Address,
               c => c.User
           )).ToList();

        }
        private List<(Charger Charger, double Distance)> FilterChargersByDistance(List<Charger> chargers, NearChargerSearchDto searchDto)
        {
            return chargers
                .Select(charger => (
                    Charger: charger,
                    Distance: CalculateDistanceInKm(
                        searchDto.Latitude,
                        searchDto.Longitude,
                        charger.Address.Latitude,
                        charger.Address.Longitude)))
                .Where(x => x.Distance <= searchDto.SearchRangeInKm)
                .OrderBy(x => x.Distance)
                .ToList();
        }
        private List<NearChargerDto> MapToDto(List<(Charger Charger, double Distance)> paginatedChargers)
        {
            var result = _mapper.Map<List<NearChargerDto>>(paginatedChargers.Select(x => x.Charger).ToList());

            for (int i = 0; i < result.Count; i++)
            {
                result[i].DistanceInKm = Math.Round(paginatedChargers[i].Distance, 2);
            }

            return result;
        }
        private string? GetCurrentUserId()
        {
            return _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        private async Task<Charger?> GetChargerById(int chargerId)
        {
            var result = await _unitOfWork.GetRepository<Charger, int>().GetAllWithIncludeAsync(
                c => c.Id == chargerId && !c.IsDeleted && c.IsActive,
                false,
                c => c.Address,
                c => c.Capacity,
                c => c.PriceOption,
                c => c.Protocol,
                c => c.User
            );
            return result.FirstOrDefault();
        }
        private void CalculateDistanceAndArrival(double userLat, double userLon, Charger charger, ChargerDetailsDto dto)
        {
            var distance = CalculateDistanceInKm(userLat, userLon, charger.Address.Latitude, charger.Address.Longitude);
            dto.DistanceInKm = Math.Round(distance, 2);
            dto.EstimatedArrival = EstimateTime(distance);
        }
        private void SetEstimatedPrice(double kwNeed, Charger charger, ChargerDetailsDto dto)
        {
            double chargerCapacity = charger.Capacity.kw;
            double sessionDurationHr = kwNeed / chargerCapacity;
            double estimatedCost = EstimatePrice(sessionDurationHr, charger.PriceOption.Value);
            dto.PriceEstimated = estimatedCost;

            // TimeNeeded is already set in GetChargerByIdAsync
        }
        private double CalculateDistanceInKm(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // نصف قطر الأرض بالكيلومتر
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        private double ToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }

        //private double EstimateTime(double distanceKm)
        //{
        //    double averageSpeedKmPerMin = 0.1;
        //    double minutes = Math.Ceiling(distanceKm / averageSpeedKmPerMin * 10) / 10; 
        //    return minutes;
        //}

        private double EstimateTime(double distanceKm)
        {
            // Base speed increases with longer distances
            double speedKmPerHour = 30 + (distanceKm * 1.2);

            // Upper limit (realistic)
            if (speedKmPerHour > 80)
                speedKmPerHour = 80;

            // Convert to minutes
            double timeMinutes = (distanceKm / speedKmPerHour) * 60;

            return Math.Round(timeMinutes);
        }




        private double EstimatePrice(double sessionDurationHr, decimal pricePerHour)
        {
            return Math.Round((double)pricePerHour * sessionDurationHr, 2);
        }


        // Add Charger
        private async Task AddChargerRecordAsync(AddChargerDto dto, string userId, int addressId)
        {
            // التحقق من وضع الشحن لتحديد حالة الشاحن الجديد
            var chargingModeEnabled = await _appSettingsService.IsChargingModeEnabledAsync();

            var charger = _mapper.Map<Charger>(dto);
            charger.UserId = userId;
            charger.AddressId = addressId;
            charger.Adaptor = dto.Adaptor;
            charger.IsActive = chargingModeEnabled; // Inactive لو الوضع مُعطّل

            await _unitOfWork.GetRepository<Charger, int>().AddAsync(charger);
            await _unitOfWork.SaveChangesAsync();
        }
        private async Task<ChargerAddress> CreateAddressAsync(AddChargerDto dto)
        {
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

            return address;
        }
        private async Task<bool> PriceOptionExistsAsync(int priceOptionId)
        {
            return await _unitOfWork.GetRepository<PriceOption, int>().AnyAsync(po => po.Id == priceOptionId);
        }
        private async Task<bool> CapacityExistsAsync(int capacityId)
        {
            return await _unitOfWork.GetRepository<Capacity, int>().AnyAsync(c => c.Id == capacityId);
        }
        private async Task<bool> ProtocolExistsAsync(int protocolId)
        {
            return await _unitOfWork.GetRepository<Protocol, int>().AnyAsync(p => p.Id == protocolId);
        }

        // Update Charger
        private async Task<Charger?> GetChargerWithAddressAsync(int chargerId, string userId)
        {
            var chargers = await _unitOfWork.GetRepository<Charger, int>().GetAllWithIncludeAsync(
                c => c.Id == chargerId && !c.IsDeleted && c.UserId == userId,
                false,
                c => c.Address);

            return chargers.FirstOrDefault();
        }
        private void UpdateChargerAndAddress(Charger charger, UpdateChargerDto dto)
        {
            charger.ProtocolId = dto.ProtocolId;
            charger.CapacityId = dto.CapacityId;
            charger.PriceOptionId = dto.PriceOptionId;
            charger.IsActive = dto.IsActive;

            charger.Address.Area = dto.Area;
            charger.Address.Street = dto.Street;
            charger.Address.BuildingNumber = dto.BuildingNumber;
            charger.Address.Latitude = dto.Latitude;
            charger.Address.Longitude = dto.Longitude;

            _unitOfWork.GetRepository<Charger, int>().Update(charger);
            _unitOfWork.GetRepository<ChargerAddress, int>().Update(charger.Address);
        }


    }

}
