using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Voltyks.Application.Interfaces;
using Voltyks.Application.Interfaces.ChargingRequest;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs.ChargerRequest;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using   ChargingRequestEntity =  Voltyks.Persistence.Entities.Main.ChargingRequest;


namespace Voltyks.Application.Services.ChargingRequest
{

    public class ChargingRequestService : IChargingRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseService _firebaseService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IVehicleService _vehicleService ;

        public ChargingRequestService(IUnitOfWork unitOfWork, IFirebaseService firebaseService , IHttpContextAccessor httpContext , IVehicleService vehicleService)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
            _httpContext = httpContext;
            _vehicleService = vehicleService;
        }

        public async Task<ApiResponse<ChargerDetailsDto>> SendChargingRequestAsync(SendChargingRequestDto dto)
        {
            try
            {
                var charger = await GetChargerWithIncludes(dto.ChargerId);
                if (charger == null)
                    return new ApiResponse<ChargerDetailsDto>(null, "Charger not found", false);

                var userId = GetCurrentUserId();
                if (userId == null)
                    return new ApiResponse<ChargerDetailsDto>(null, "Car owner not found", false);

                var chargingRequest = await CreateChargingRequest(userId, dto.ChargerId);

                var userOwnerChargerId = charger.UserId;
                var tokenList = await GetDeviceTokens(userOwnerChargerId);

                if (tokenList.Any())
                {
                    string title = "New Charging Request 🚗";
                    string body = $"Driver {userId} requested to charge at your station.";
                    await SendFcmNotifications(tokenList, title, body , chargingRequest.Id);
                }

                await CreateNotification(userOwnerChargerId, userId, chargingRequest.Id);

                return new ApiResponse<ChargerDetailsDto>("Charging request sent successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ChargerDetailsDto>(null, ex.Message, false);
            }
        }
        public async Task<ApiResponse<bool>> RegisterDeviceTokenAsync(DeviceTokenDto tokenDto)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(tokenDto?.DeviceToken) || string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<bool>(false, "Invalid DeviceToken or User", false);

            var alreadyExists = await CheckIfTokenExists(tokenDto.DeviceToken, userId);
            if (!alreadyExists)
            {
                await SaveNewDeviceToken(tokenDto.DeviceToken, userId);
            }

            return new ApiResponse<bool>(true, "Token registered", true);
        }
        public async Task<ApiResponse<bool>> AcceptRequestAsync(TransRequest dto)
        {
            try
            {
                var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                    .GetAllWithIncludeAsync(
                        r => r.Id == dto.RequestId,
                        false,
                        r => r.Charger, r => r.Charger.User, r => r.CarOwner)) // include car owner
                    .FirstOrDefault();

                if (request == null)
                    return new ApiResponse<bool>(false, "Charging request not found", false);

                // optional: authorize the current user if needed (station owner)

                request.Status = "accepted";
                request.RespondedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<ChargingRequestEntity, int>().Update(request);
                await _unitOfWork.SaveChangesAsync();

                // 🔔 Notify the car owner
                var carOwnerTokens = request.CarOwner.DeviceTokens?
                    .Where(t => t.UserId == request.UserId)
                    .Select(t => t.Token)
                    .ToList();

                if (carOwnerTokens != null && carOwnerTokens.Any())
                {
                    string title = "Charging Request Accepted ";
                    string body = $"Your request to charge at {request.Charger.User.FullName}'s station has been accepted.";

                    foreach (var token in carOwnerTokens)
                    {
                        await _firebaseService.SendNotificationAsync(token, title, body, request.Id);
                    }
                }

                var notifi = new Notification()
                {
                    Title = "Charging Request Accepted ",
                    Body = $"Your request to charge at {request.Charger.User.FullName}'s station has been accepted.",
                    IsRead = false,
                    SentAt = DateTime.UtcNow,
                    UserId = request.UserId,
                    RelatedRequestId = request.Id,
                    UserTypeId = 2 // "VehicleOwner"


                    //  add new property UserSenderId
                };

                await _unitOfWork.GetRepository<Notification, int>().AddAsync(notifi);


                return new ApiResponse<bool>(true, "Charging request accepted and user notified", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
        public async Task<ApiResponse<bool>> RejectRequestAsync(TransRequest dto)
        {
            try
            {
                var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                    .GetAllWithIncludeAsync(
                        r => r.Id == dto.RequestId,
                        false,
                        r => r.Charger, r => r.Charger.User, r => r.CarOwner)) // include CarOwner
                    .FirstOrDefault();

                if (request == null)
                    return new ApiResponse<bool>(false, "Charging request not found", false);

                request.Status = "rejected";
                request.RespondedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<ChargingRequestEntity, int>().Update(request);
                await _unitOfWork.SaveChangesAsync();

                // 🔔 Notify the car owner
                var carOwnerTokens = request.CarOwner.DeviceTokens?
                    .Where(t => t.UserId == request.UserId)
                    .Select(t => t.Token)
                    .ToList();

                if (carOwnerTokens != null && carOwnerTokens.Any())
                {
                    string title = "Charging Request Rejected ❌";
                    string body = $"Your request to charge at {request.Charger.User.FullName}'s station was rejected.";

                    foreach (var token in carOwnerTokens)
                    {
                        await _firebaseService.SendNotificationAsync(token, title, body , request.Id);
                    }
                }
                var notifi = new Notification()
                {
                    Title = "Charging Request Rejected ❌",
                    Body = $"Your request to charge at {request.Charger.User.FullName}'s station was rejected.",
                    IsRead = false,
                    SentAt = DateTime.UtcNow,
                    UserId = request.UserId,
                    RelatedRequestId = request.Id,
                    UserTypeId = 2 // "VehicleOwner"

                    //  add new property UserSenderId
                };

                await _unitOfWork.GetRepository<Notification, int>().AddAsync(notifi);


                return new ApiResponse<bool>(true, "Charging request rejected and car owner notified", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
        public async Task<ApiResponse<bool>> ConfirmRequestAsync(TransRequest dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return new ApiResponse<bool>(false, "Unauthorized", false);

                var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                    .GetAllWithIncludeAsync(
                        r => r.Id == dto.RequestId,
                        false,
                        r => r.CarOwner, r => r.Charger, r => r.Charger.User)) // include both users
                    .FirstOrDefault();

                if (request == null)
                    return new ApiResponse<bool>(false, "Charging request not found", false);

                if (request.CarOwner.Id != userId)
                    return new ApiResponse<bool>(false, "Not your request", false);

                request.Status = "confirmed";
                request.ConfirmedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<ChargingRequestEntity, int>().Update(request);
                await _unitOfWork.SaveChangesAsync();

                // 🔔 Notify the station owner
                var stationOwnerTokens = request.Charger.User.DeviceTokens?
                    .Where(t => t.UserId == request.UserId)
                    .Select(t => t.Token)
                    .ToList();

                if (stationOwnerTokens != null && stationOwnerTokens.Any())
                {
                    string title = "Request Confirmed ✅";
                    string body = $"The driver {request.CarOwner.FullName} confirmed the charging session at your station.";

                    foreach (var token in stationOwnerTokens)
                    {
                        await _firebaseService.SendNotificationAsync(token, title, body , request.Id);
                    }
                }
                var notifi = new Notification()
                {
                    Title = "Request Confirmed ✅",
                    Body = $"The driver {request.CarOwner.FullName} confirmed the charging session at your station.",
                    IsRead = false,
                    SentAt = DateTime.UtcNow,
                    UserId = request.UserId,
                    RelatedRequestId = request.Id,
                    UserTypeId = 1 // "ChargerOwner"


                    //  add new property UserSenderId
                };

                await _unitOfWork.GetRepository<Notification, int>().AddAsync(notifi);


                return new ApiResponse<bool>(true, "Charging request confirmed and station owner notified", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }

        //public async Task<ApiResponse<ChargingRequestDetailsDto>> GetRequestDetailsAsync(int requestId)
        //{
        //    try
        //    {
        //        var request = await GetRequestWithDetailsAsync(requestId);

        //        if (request == null)
        //            return new ApiResponse<ChargingRequestDetailsDto>(null, "Charging request not found", false);

        //        var dto = MapToDto(request);

        //        return new ApiResponse<ChargingRequestDetailsDto>(dto, "Charging request details fetched", true);
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<ChargingRequestDetailsDto>(null, ex.Message, false);
        //    }
        //}
        //public async Task<ApiResponse<ChargingRequestDetailsDto>> GetRequestDetailsAsync(RequestDetailsDto dto)
        //{
        //    try
        //    {
        //        var request = await GetRequestWithDetailsAsync(dto.RequestId);

        //        if (request == null)
        //            return new ApiResponse<ChargingRequestDetailsDto>(null, "Charging request not found", false);

        //        // 🧠 حساب المسافة بين السيارة والشاحن
        //        string estimatedArrival = "N/A";
        //        if (dto.Latitude.HasValue && dto.Longitude.HasValue && request.Charger.Address?.Latitude != null && request.Charger.Address?.Longitude != null)
        //        {
        //            double distanceKm = CalculateDistance(
        //                dto.Latitude.Value,
        //                dto.Longitude.Value,
        //                request.Charger.Address.Latitude, 
        //                request.Charger.Address.Longitude 
        //            );

        //            double estimatedMinutes = (distanceKm / 40.0) * 60.0; 
        //            estimatedArrival = $"~ {Math.Ceiling(estimatedMinutes)} min";
        //        }


        //        string estimatedPrice = request.Charger.PriceOption != null
        //            ? $"{request.Charger.PriceOption.Value} EGP/hour"
        //            : "N/A";

        //        var response = new ChargingRequestDetailsDto
        //        {
        //            RequestId = request.Id,
        //            Status = request.Status,
        //            RequestedAt = request.RequestedAt,                
        //            CarOwnerId = request.CarOwner.Id, 


        //            CarOwnerName = new StringBuilder().Append(request.CarOwner.FirstName).Append(" ").Append(request.CarOwner.LastName).ToString(),
        //            StationOwnerId = request.Charger.User.Id,                   
        //            StationOwnerName =  new StringBuilder().Append(request.Charger.User.FirstName).Append(" ") .Append(request.Charger.User.LastName).ToString(),
        //            ChargerId = request.ChargerId,
        //            Protocol = request.Charger.Protocol?.Name ?? "Unknown",
        //            CapacityKw = request.Charger.Capacity?.kw ?? 0,
        //            PricePerHour = request.Charger.PriceOption != null
        //                ? $"{request.Charger.PriceOption.Value} EGP"
        //                : "N/A",
        //            AdapterAvailability = request.Charger.Adaptor == true ? "Available" : "Not Available",
        //            Area = request.Charger.Address?.Area ?? "N/A",
        //            Street = request.Charger.Address?.Street ?? "N/A",
        //            EstimatedArrival = estimatedArrival,
        //            EstimatedPrice = estimatedPrice,




        //        };

        //        return new ApiResponse<ChargingRequestDetailsDto>(response, "Charging request details fetched", true);
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<ChargingRequestDetailsDto>(null, ex.Message, false);
        //    }
        //}
        public async Task<ApiResponse<ChargingRequestDetailsDto>> GetRequestDetailsAsync(RequestDetailsDto dto)
        {
            try
            {
                var request = await GetRequestWithDetailsAsync(dto.RequestId);

                if (request == null)
                    return new ApiResponse<ChargingRequestDetailsDto>(null, "Charging request not found", false);

                // 🧠 حساب المسافة بين السيارة والشاحن
                string estimatedArrival = "N/A";
                double distanceKm = 0;
                if (dto.Latitude.HasValue && dto.Longitude.HasValue && request.Charger.Address?.Latitude != null && request.Charger.Address?.Longitude != null)
                {
                    distanceKm = CalculateDistance(
                        dto.Latitude.Value,
                        dto.Longitude.Value,
                        request.Charger.Address.Latitude,
                        request.Charger.Address.Longitude
                    );

                    double estimatedMinutes = (distanceKm / 40.0) * 60.0;
                    estimatedArrival = $"~ {Math.Ceiling(estimatedMinutes)} min";
                }

                string estimatedPrice = request.Charger.PriceOption != null
                    ? $"{request.Charger.PriceOption.Value} EGP/hour"
                    : "N/A";

              

                var vehicles = await _vehicleService.GetVehiclesByUserIdAsync();

                // إذا كان المستخدم يمتلك سيارات متعددة، سنختار السيارة الأولى أو نضع منطق لاختيار السيارة المناسبة
                var vehicle = vehicles?.Data.FirstOrDefault(); // اختيار السيارة الأولى

                var response = new ChargingRequestDetailsDto
                {
                    RequestId = request.Id,
                    Status = request.Status,
                    RequestedAt = request.RequestedAt,
                    CarOwnerId = request.CarOwner.Id,
                    CarOwnerName = new StringBuilder().Append(request.CarOwner.FirstName).Append(" ").Append(request.CarOwner.LastName).ToString(),

                    // إضافة معلومات السيارة باستخدام VehicleDto
                    VehicleBrand = vehicle?.BrandName ?? "Unknown", // اسم العلامة التجارية
                    VehicleModel = vehicle?.ModelName ?? "Unknown", // اسم الطراز
                    VehicleColor = vehicle?.Color ?? "Unknown", // اللون
                    VehiclePlate = vehicle?.Plate ?? "Unknown", // لو كان اسم السيارة عبارة عن رقم اللوحة

                    StationOwnerId = request.Charger.User.Id,
                    StationOwnerName = new StringBuilder().Append(request.Charger.User.FirstName).Append(" ").Append(request.Charger.User.LastName).ToString(),
                    ChargerId = request.ChargerId,
                    Protocol = request.Charger.Protocol?.Name ?? "Unknown",
                    CapacityKw = request.Charger.Capacity?.kw ?? 0,
                    PricePerHour = request.Charger.PriceOption != null
                ? $"{request.Charger.PriceOption.Value} EGP"
                : "N/A",
                    AdapterAvailability = request.Charger.Adaptor == true ? "Available" : "Not Available",
                    Area = request.Charger.Address?.Area ?? "N/A",
                    Street = request.Charger.Address?.Street ?? "N/A",
                    EstimatedArrival = estimatedArrival,
                    EstimatedPrice = estimatedPrice,
                    DistanceInKm = distanceKm // إضافة المسافة إلى الاستجابة
                };

                return new ApiResponse<ChargingRequestDetailsDto>(response, "Charging request details fetched", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ChargingRequestDetailsDto>(null, ex.Message, false);
            }
        }



        // SendChargingRequestAsync ===> Helper Private Mehtods
        private async Task<Charger?> GetChargerWithIncludes(int chargerId)
        {
            return (await _unitOfWork.GetRepository<Charger, int>()
                .GetAllWithIncludeAsync(
                    c => c.Id == chargerId,
                    false,
                    c => c.Address,
                    c => c.Protocol,
                    c => c.Capacity,
                    c => c.PriceOption,
                    c => c.User))
                .FirstOrDefault();
        }
        private async Task<ChargingRequestEntity> CreateChargingRequest(string userId, int chargerId)
        {
            var request = new ChargingRequestEntity
            {
                UserId = userId,
                ChargerId = chargerId,
                RequestedAt = DateTime.UtcNow,
                Status = "pending"
            };

            await _unitOfWork.GetRepository<ChargingRequestEntity, int>().AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return request;
        }
        private async Task<List<string>> GetDeviceTokens(string userId)
        {
            var tokens = await _unitOfWork.GetRepository<DeviceToken, int>()
                .GetAllAsync(t => t.UserId == userId);
            return tokens.Select(t => t.Token).ToList();
        }
        private async Task SendFcmNotifications(List<string> tokens, string title, string body , int chargingRequestID)
        {
            foreach (var token in tokens)
            {
                await _firebaseService.SendNotificationAsync(token, title, body , chargingRequestID);
            }
        }
        private async Task CreateNotification(string receiverUserId, string senderUserId, int relatedRequestId)
        {
            var notification = new Notification
            {
                Title = "New Charging Request 🚗",
                Body = $"Driver {senderUserId} requested to charge at your station.",
                IsRead = false,
                SentAt = DateTime.UtcNow,
                UserId = receiverUserId,
                RelatedRequestId = relatedRequestId,
                UserTypeId = 1 // ChargerOwner
            };

            await _unitOfWork.GetRepository<Notification, int>().AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();
        }


        // RegisterDeviceTokenAsync ===> Helper Private Mehtods
        private async Task<bool> CheckIfTokenExists(string token, string userId)
        {
            var existing = await _unitOfWork.GetRepository<DeviceToken, int>()
                .GetFirstOrDefaultAsync(t => t.Token == token && t.UserId == userId);
            return existing != null;
        }
        private async Task SaveNewDeviceToken(string token, string userId)
        {
            var newToken = new DeviceToken
            {
                Token = token,
                UserId = userId,
                RoleContext = "Owner", // ثابت حاليًا
                RegisteredAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<DeviceToken, int>().AddAsync(newToken);
            await _unitOfWork.SaveChangesAsync();
        }
        private string? GetCurrentUserId()
        {
            return _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }



        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // Radius of earth in KM
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c;

            return distance;
        }
        private double DegreesToRadians(double deg) => deg * (Math.PI / 180);

        // GetRequestDetailsAsync ===> Helper Private Mehtods
        private async Task<ChargingRequestEntity?> GetRequestWithDetailsAsync(int requestId)
        {
            return (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                .GetAllWithIncludeAsync(
                    r => r.Id == requestId,
                    false,
                    r => r.CarOwner,
                    r => r.Charger,
                    r => r.Charger.User,
                    r => r.Charger.Address,
                    r => r.Charger.Protocol,
                    r => r.Charger.Capacity,
                    r => r.Charger.PriceOption
                )).FirstOrDefault();
        }


        //private ChargingRequestDetailsDto MapToDto(ChargingRequestEntity request)
        //{
        //    return new ChargingRequestDetailsDto
        //    {
        //        RequestId = request.Id,
        //        Status = request.Status,
        //        RequestedAt = request.RequestedAt,
        //        RespondedAt = request.RespondedAt,
        //        ConfirmedAt = request.ConfirmedAt,

        //        CarOwnerId = request.CarOwner.Id,
        //        CarOwnerName = request.CarOwner.FullName,

        //        StationOwnerId = request.Charger.User.Id,
        //        StationOwnerName = request.Charger.User.FullName,

        //        ChargerId = request.ChargerId,
        //        Protocol = request.Charger.Protocol?.Name ?? "Unknown",
        //        CapacityKw = request.Charger.Capacity?.kw ?? 0,
        //        PricePerHour = request.Charger.PriceOption != null
        //            ? $"{request.Charger.PriceOption.Value} EGP"
        //            : "N/A",
        //        AdapterAvailability = request.Charger.Adaptor == true ? "Available" : "Not Available",

        //        Area = request.Charger.Address?.Area ?? "N/A",
        //        Street = request.Charger.Address?.Street ?? "N/A",

        //        Rating = request.Charger.AverageRating,
        //        RatingCount = request.Charger.RatingCount,

        //        EstimatedArrival = "N/A",
        //        EstimatedPrice = "Estimated"
        //    };
        //}
    }



}
