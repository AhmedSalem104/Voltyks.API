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

                var token = charger.User.DeviceTokens?.FirstOrDefault()?.Token;
                if (!string.IsNullOrEmpty(token))
                {
                    string title = "New Charging Request 🚗";
                    string body = $"Driver {userId} requested to charge at your station.";
                    await _firebaseService.SendNotificationAsync(token, title, body);
                }

                var chargerDetails = new ChargerDetailsDto
                {
                    FullName = charger.User.FullName,
                    Rating = charger.AverageRating,
                    RatingCount = charger.RatingCount,
                    Area = charger.Address.Area,
                    Street = charger.Address.Street,
                    DistanceInKm = 0,
                    EstimatedArrival = "N/A",
                    Protocol = charger.Protocol.Name,
                    Capacity = charger.Capacity.kw,
                    PricePerHour = $"{charger.PriceOption.Value} EGP",
                    AdapterAvailability = charger.Adaptor == true ? "Available" : "Not Available",
                    PriceEstimated = "Estimated"
                };

                return new ApiResponse<ChargerDetailsDto>(chargerDetails, "Charging request sent successfully", true);
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

            var existing = await _unitOfWork.GetRepository<DeviceToken, int>()
                .GetFirstOrDefaultAsync(t => t.Token == tokenDto.DeviceToken && t.UserId == userId);

            if (existing == null)
            {
                await _unitOfWork.GetRepository<DeviceToken, int>().AddAsync(new DeviceToken
                {
                    Token = tokenDto.DeviceToken,
                    UserId = userId,
                    RegisteredAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();
            }

            return new ApiResponse<bool>(true, "Token registered", true);
        }



        //public async Task AcceptRequestAsync(int requestId, string ownerId)
        //{
        //    var request = (await _unitOfWork.GetRepository<ChargingRequest, int>()
        //        .GetAllWithIncludeAsync(r => r.Id == requestId, includes: r => r.Charger))
        //        .FirstOrDefault();

        //    if (request == null)
        //        throw new Exception("Charging request not found");

        //    if (request.Charger.UserId != ownerId)
        //        throw new UnauthorizedAccessException("Not authorized");

        //    request.Status = "accepted";
        //    request.RespondedAt = DateTime.UtcNow;

        //    _unitOfWork.GetRepository<ChargingRequest, int>().Update(request);
        //    await _unitOfWork.SaveChangesAsync();
        //}

        //public async Task RejectRequestAsync(int requestId, string ownerId)
        //{
        //    var request = (await _unitOfWork.GetRepository<ChargingRequest, int>()
        //        .GetAllWithIncludeAsync(r => r.Id == requestId, includes: r => r.Charger))
        //        .FirstOrDefault();

        //    if (request == null)
        //        throw new Exception("Charging request not found");

        //    if (request.Charger.UserId != ownerId)
        //        throw new UnauthorizedAccessException("Not authorized to reject this request");

        //    request.Status = "rejected";
        //    request.RespondedAt = DateTime.UtcNow;

        //    _unitOfWork.GetRepository<ChargingRequest, int>().Update(request);
        //    await _unitOfWork.SaveChangesAsync();
        //}

        //public async Task ConfirmRequestAsync(int requestId, string carOwnerId)
        //{
        //    var request = await _unitOfWork.GetRepository<ChargingRequest, int>()
        //        .GetFirstOrDefaultAsync(r => r.Id == requestId);

        //    if (request == null)
        //        throw new Exception("Charging request not found");

        //    if (request.CarOwnerId != carOwnerId)
        //        throw new UnauthorizedAccessException("Not your request");

        //    request.Status = "confirmed";
        //    request.ConfirmedAt = DateTime.UtcNow;

        //    _unitOfWork.GetRepository<ChargingRequest, int>().Update(request);
        //    await _unitOfWork.SaveChangesAsync();
        //}

        //public async Task<ChargingRequestDetailsDto> GetRequestDetailsAsync(int requestId)
        //{
        //    var request = (await _unitOfWork.GetRepository<ChargingRequest, int>()
        //        .GetAllWithIncludeAsync(r => r.Id == requestId, includes: r => r.CarOwner, r => r.Charger, r => r.Charger.User))
        //        .FirstOrDefault();

        //    if (request == null)
        //        throw new Exception("Charging request not found");

        //    return new ChargingRequestDetailsDto
        //    {
        //        RequestId = request.Id,
        //        Status = request.Status,
        //        CarOwnerName = request.CarOwner.FullName,
        //        ChargerOwnerName = request.Charger.User.FullName,
        //        RequestedAt = request.RequestedAt,
        //        RespondedAt = request.RespondedAt,
        //        ConfirmedAt = request.ConfirmedAt
        //    };
        //}

        //public Task CreateChargingRequestAsync(SendChargingRequestDto dto)
        //{
        //    throw new NotImplementedException();
        //}


        private string? GetCurrentUserId()
        {
            return _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }



}
