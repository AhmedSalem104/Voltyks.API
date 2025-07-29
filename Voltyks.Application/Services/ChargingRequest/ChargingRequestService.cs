using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        public ChargingRequestService(IUnitOfWork unitOfWork, IFirebaseService firebaseService , IHttpContextAccessor httpContext)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
            _httpContext = httpContext;
        }

        public async Task<ApiResponse<ChargerDetailsDto>> SendChargingRequestAsync(SendChargingRequestDto dto)
        {
            try
            {
                var charger = (await _unitOfWork.GetRepository<Charger, int>()
                    .GetAllWithIncludeAsync(
                        c => c.Id == dto.ChargerId,
                        false,
                        c => c.Address, c => c.Protocol, c => c.Capacity, c => c.PriceOption, c => c.User
                    )).FirstOrDefault();

                if (charger == null)
                    return new ApiResponse<ChargerDetailsDto>(null, "Charger not found", false);

                var userId = GetCurrentUserId();

                if (userId == null)
                    return new ApiResponse<ChargerDetailsDto>(null, "Car owner not found", false);

                var chargingRequest = new ChargingRequestEntity
                {
                    UserId = userId,
                    ChargerId = dto.ChargerId,
                    RequestedAt = DateTime.UtcNow,
                    Status = "pending"
                };

                await _unitOfWork.GetRepository<ChargingRequestEntity, int>().AddAsync(chargingRequest);
                await _unitOfWork.SaveChangesAsync();

                var userOwnerCharger = await _unitOfWork.GetRepository<Charger, int>().GetAsync(charger.Id);
                var userOwnerChargerId = userOwnerCharger.UserId;

                var tokens = charger.User.DeviceTokens?
                         .Where(t => t.UserId == userOwnerChargerId)
                         .Select(t => t.Token)
                         .ToList();

                if (tokens != null && tokens.Any())
                {
                    string title = "New Charging Request 🚗";
                    string body = $"Driver {userId} requested to charge at your station.";

                    foreach (var token in tokens)
                    {
                        await _firebaseService.SendNotificationAsync(token, title, body);
                    }
                }

                var notifi = new Notification()
                {
                    Title = "New Charging Request 🚗",
                    Body = $"Driver {userId} requested to charge at your station.",
                    IsRead = false,
                    SentAt = DateTime.UtcNow,
                    UserId = userOwnerChargerId,
                    RelatedRequestId = chargingRequest.Id,
                    UserTypeId = 1 // "ChargerOwner"

                    //  add new property UserSenderId
                };

                await _unitOfWork.GetRepository<Notification, int>().AddAsync(notifi);

           


                //var chargerDetails = new ChargerDetailsDto
                //{
                //    FullName = charger.User.FullName,
                //    Rating = charger.AverageRating,
                //    RatingCount = charger.RatingCount,
                //    Area = charger.Address.Area,
                //    Street = charger.Address.Street,
                //    DistanceInKm = 0,
                //    EstimatedArrival = "N/A",
                //    Protocol = charger.Protocol.Name,
                //    Capacity = charger.Capacity.kw,
                //    PricePerHour = $"{charger.PriceOption.Value} EGP",
                //    AdapterAvailability = charger.Adaptor == true ? "Available" : "Not Available",
                //    PriceEstimated = "Estimated"
                //};

                return new ApiResponse<ChargerDetailsDto>("Charging request sent successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ChargerDetailsDto>(null, ex.Message, false);
            }
        }
        public async Task<ApiResponse<bool>> RegisterDeviceTokenAsync(DeviceTokenDto tokenDto)
        {
            var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(tokenDto?.DeviceToken) || string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<bool>(false, "Invalid DeviceToken or User", false);

            var existing = await _unitOfWork.GetRepository<DeviceToken, int>().GetFirstOrDefaultAsync(t =>
               t.Token == tokenDto.DeviceToken &&
               t.UserId == userId) ;
               //t.RoleContext == tokenDto.RoleContext);

            if (existing == null)
            {
                await _unitOfWork.GetRepository<DeviceToken, int>().AddAsync(new DeviceToken
                {
                    Token = tokenDto.DeviceToken,
                    UserId = userId,
                   // RoleContext = tokenDto.RoleContext,
                    RegisteredAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();
            }
            // Update DeviceToken For Current User .

            return new ApiResponse<bool>(true, "Token registered", true);
        }
        public async Task<ApiResponse<bool>> AcceptRequestAsync(int requestId)
        {
            try
            {
                var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                    .GetAllWithIncludeAsync(
                        r => r.Id == requestId,
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
                        await _firebaseService.SendNotificationAsync(token, title, body);
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
        public async Task<ApiResponse<bool>> RejectRequestAsync(int requestId)
        {
            try
            {
                var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                    .GetAllWithIncludeAsync(
                        r => r.Id == requestId,
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
                        await _firebaseService.SendNotificationAsync(token, title, body);
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
        public async Task<ApiResponse<bool>> ConfirmRequestAsync(int requestId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return new ApiResponse<bool>(false, "Unauthorized", false);

                var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                    .GetAllWithIncludeAsync(
                        r => r.Id == requestId,
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
                        await _firebaseService.SendNotificationAsync(token, title, body);
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
        public async Task<ApiResponse<ChargingRequestDetailsDto>> GetRequestDetailsAsync(int requestId)
        {
            try
            {
                var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                    .GetAllWithIncludeAsync(
                        r => r.Id == requestId,
                        false,
                        r => r.CarOwner,                    // Car owner
                        r => r.Charger,
                        r => r.Charger.User,           // Station owner
                        r => r.Charger.Address,
                        r => r.Charger.Protocol,
                        r => r.Charger.Capacity,
                        r => r.Charger.PriceOption
                    )).FirstOrDefault();

                if (request == null)
                    return new ApiResponse<ChargingRequestDetailsDto>(null, "Charging request not found", false);

                var dto = new ChargingRequestDetailsDto
                {
                    RequestId = request.Id,
                    Status = request.Status,
                    RequestedAt = request.RequestedAt,
                    RespondedAt = request.RespondedAt,
                    ConfirmedAt = request.ConfirmedAt,

                    CarOwnerId = request.CarOwner.Id,
                    CarOwnerName = request.CarOwner.FullName,

                    StationOwnerId = request.Charger.User.Id,
                    StationOwnerName = request.Charger.User.FullName,

                    ChargerId = request.ChargerId,
                    Protocol = request.Charger.Protocol?.Name ?? "Unknown",
                    CapacityKw = request.Charger.Capacity?.kw ?? 0,
                    PricePerHour = request.Charger.PriceOption != null
                        ? $"{request.Charger.PriceOption.Value} EGP"
                        : "N/A",
                    AdapterAvailability = request.Charger.Adaptor == true ? "Available" : "Not Available",

                    Area = request.Charger.Address?.Area ?? "N/A",
                    Street = request.Charger.Address?.Street ?? "N/A",

                    Rating = request.Charger.AverageRating,
                    RatingCount = request.Charger.RatingCount,

                    EstimatedArrival = "N/A", // يمكنك حسابها لاحقًا بناءً على موقع السيارة مثلاً
                    EstimatedPrice = "Estimated"
                };

                return new ApiResponse<ChargingRequestDetailsDto>(dto, "Charging request details fetched", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ChargingRequestDetailsDto>(null, ex.Message, false);
            }
        }

        private string? GetCurrentUserId()
        {
            return _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }



}
