using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Voltyks.Application.Interfaces;
using Voltyks.Application.Interfaces.ChargingRequest;
using Voltyks.Application.Interfaces.FeesConfig;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.SignalR;
using Voltyks.Application.Utilities;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.ChargerRequest;
using Voltyks.Core.Enums;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Data;
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
        private readonly IFeesConfigService _feesConfigService;
        private readonly VoltyksDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISignalRService _signalRService;



        public ChargingRequestService(IUnitOfWork unitOfWork, IFirebaseService firebaseService , IHttpContextAccessor httpContext , IVehicleService vehicleService, IFeesConfigService feesConfigService, VoltyksDbContext db, IHttpClientFactory httpClientFactory, ISignalRService signalRService)
        {
            _unitOfWork = unitOfWork;
            _firebaseService = firebaseService;
            _httpContext = httpContext;
            _vehicleService = vehicleService;
            _feesConfigService = feesConfigService;
            _db = db;
            _httpClientFactory = httpClientFactory;
            _signalRService = signalRService;
        }

        public async Task<ApiResponse<NotificationResultDto>> SendChargingRequestAsync(SendChargingRequestDto dto)
        {
            try
            {
                // Safety net: التحقق من صحة الموقع - رفض (0,0) لأنه يعني فشل GPS
                if (dto.Latitude == 0 && dto.Longitude == 0)
                {
                    return new ApiResponse<NotificationResultDto>(
                        null,
                        "Invalid location. Please enable GPS and try again.",
                        false
                    );
                }

                var charger = await GetChargerWithIncludes(dto.ChargerId);
                if (charger == null)
                    return new ApiResponse<NotificationResultDto>(null, "Charger not found", false);

                var userId = GetCurrentUserId();
                if (userId == null)
                    return new ApiResponse<NotificationResultDto>(null, "Car owner not found", false);

                // Check if vehicle owner already has an active process
                var vehicleOwner = await _db.Set<AppUser>().FindAsync(userId);
                if (vehicleOwner?.CurrentActivities?.Count > 0)
                {
                    // Smart validation: verify the IDs are actually still active
                    var activeProcessIds = vehicleOwner.CurrentActivities.ToList();
                    var hasActuallyActiveProcess = await _db.Set<Process>()
                        .AnyAsync(p => activeProcessIds.Contains(p.Id)
                            && p.Status != ProcessStatus.Completed
                            && p.Status != ProcessStatus.Aborted);

                    if (hasActuallyActiveProcess)
                        return new ApiResponse<NotificationResultDto>(null, "You already have an active charging process", false);

                    // Auto-cleanup: remove stale entries
                    vehicleOwner.CurrentActivities = new List<int>();
                    vehicleOwner.IsAvailable = true;
                    _db.Update(vehicleOwner);
                    await _db.SaveChangesAsync();
                }

                // Check if charger owner already has an active process
                if (charger.User?.CurrentActivities?.Count > 0)
                {
                    var activeProcessIds = charger.User.CurrentActivities.ToList();
                    var hasActuallyActiveProcess = await _db.Set<Process>()
                        .AnyAsync(p => activeProcessIds.Contains(p.Id)
                            && p.Status != ProcessStatus.Completed
                            && p.Status != ProcessStatus.Aborted);

                    if (hasActuallyActiveProcess)
                        return new ApiResponse<NotificationResultDto>(null, "Charger owner is currently busy", false);

                    // Auto-cleanup: remove stale entries
                    charger.User.CurrentActivities = new List<int>();
                    charger.User.IsAvailable = true;
                    _db.Update(charger.User);
                    await _db.SaveChangesAsync();
                }

                // 1) أنشئ الطلب
                var chargingRequest = await CreateChargingRequest(userId, dto.ChargerId, dto.KwNeeded, dto.CurrentBatteryPercentage , dto.Latitude,dto.Longitude , charger.UserId ,  charger);

                // 2) جهّز بيانات الإشعار
                var recipientUserId = charger.UserId; // صاحب المحطة
                var title = "New Charging Request 🚗";
                var body = $"Driver requested to charge at your station.";
                var notificationType = NotificationTypes.VehicleOwner_RequestCharger; // ثوابت
                var userTypeId = (int)NotificationUserType.ChargerOwner;              // 1

                // 3) إرسال + تسجيل + إرجاع DTO
                var result = await SendAndPersistNotificationAsync(
                    receiverUserId: recipientUserId,
                    requestId: chargingRequest.Id,
                    title: title,
                    body: body,
                    notificationType: notificationType,
                    userTypeId: userTypeId
                );

                // 4) SignalR Real-time notification
                await _signalRService.SendNewRequestAsync(chargingRequest.Id, recipientUserId, new
                {
                    requestId = chargingRequest.Id,
                    chargerId = dto.ChargerId,
                    kwNeeded = dto.KwNeeded,
                    status = "pending",
                    timerStartedAt = DateTimeHelper.GetEgyptTime(),
                    timerDurationMinutes = 5
                });

                return new ApiResponse<NotificationResultDto>(result, "Charging request sent successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<NotificationResultDto>(null, ex.Message, false);
            }
        }
        public async Task<ApiResponse<bool>> RegisterDeviceTokenAsync(DeviceTokenDto tokenDto)
        {
            var userId = GetCurrentUserId();
            var token = tokenDto?.DeviceToken?.Trim();

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<bool>(false, "Invalid DeviceToken or User", false);

           

            var ok = await SaveOrUpdateDeviceTokenAsync(token, userId);
            return new ApiResponse<bool>(ok, "Token registered", ok);
        }
        public async Task<ApiResponse<NotificationResultDto>> AcceptRequestAsync(TransRequest dto)
        {
            try
            {
                var request = await GetAndUpdateRequestAsync(dto, RequestStatuses.Accepted);
                if (request == null)
                    return new ApiResponse<NotificationResultDto>(null, "Charging request not found", false);

                var recipientUserId = request.CarOwner?.Id; // VehicleOwner
                var title = "Charging Request Accepted";
                var body = $"Your request to charge at {request.Charger?.User?.FullName}'s station has been accepted.";
                var notificationType = "ChargerOwner_AcceptRequest";

                // Timer data للـ FCM notification
                var timerData = new Dictionary<string, string>
                {
                    ["timerStartedAt"] = request.RespondedAt?.ToString("o") ?? "",
                    ["timerDurationMinutes"] = "10"
                };

                var result = await SendAndPersistNotificationAsync(
                    receiverUserId: recipientUserId!,
                    requestId: request.Id,
                    title: title,
                    body: body,
                    notificationType: notificationType,
                    userTypeId: 2, // VehicleOwner
                    extraData: timerData
                );

                // SignalR Real-time notification لصاحب العربية
                await _signalRService.SendRequestAcceptedAsync(request.Id, recipientUserId!, new
                {
                    requestId = request.Id,
                    chargerOwnerName = request.Charger?.User?.FullName,
                    status = "accepted",
                    timerStartedAt = request.RespondedAt,
                    timerDurationMinutes = 10
                });

                // SignalR لصاحب الشاحن أيضاً لتوحيد التايمر
                await _signalRService.SendRequestAcceptedAsync(request.Id, request.Charger?.User?.Id!, new
                {
                    requestId = request.Id,
                    carOwnerName = request.CarOwner?.FullName,
                    status = "accepted",
                    timerStartedAt = request.RespondedAt,
                    timerDurationMinutes = 10
                });

                // إضافة معلومات التايمر في الـ response لصاحب الشاحن
                var resultWithTimer = result with
                {
                    ExtraData = new
                    {
                        timerStartedAt = request.RespondedAt,
                        timerDurationMinutes = 10
                    }
                };

                return new ApiResponse<NotificationResultDto>(resultWithTimer, "Charging request accepted", true);

            }
            catch (Exception ex)
            {
                return new ApiResponse<NotificationResultDto>(null, ex.Message, false);
            }
        }
        public async Task<ApiResponse<List<NotificationResultDto>>> RejectRequestsAsync(RejectRequestDto dto)
        {
            try
            {
                var requestIds = dto?.RequestIds;
                if (requestIds == null || requestIds.Count == 0)
                    return new ApiResponse<List<NotificationResultDto>>(null, "No requests provided", false);

                var results = new List<NotificationResultDto>();

                // Fix N+1 query problem: fetch all requests in one query
                var ids = requestIds.Select(r => r.RequestId).ToList();
                var requests = await _db.Set<ChargingRequestEntity>()
                    .Include(r => r.CarOwner)
                    .Include(r => r.Charger)
                        .ThenInclude(c => c.User)
                    .Where(r => ids.Contains(r.Id))
                    .ToListAsync();

                foreach (var request in requests)
                {
                    // Update request status to rejected
                    request.Status = "Rejected";
                    _db.Entry(request).Property(r => r.Status).IsModified = true;

                    var recipientUserId = request.CarOwner?.Id;
                    if (string.IsNullOrWhiteSpace(recipientUserId))
                        continue;

                    var title = "Charging Request Rejected ❌";
                    var stationOwnerName = request.Charger?.User?.FullName ?? "the station";
                    var body = $"Your request to charge at {stationOwnerName}'s station was rejected.";
                    var notificationType = "ChargerOwner_RejectRequest";

                    var sent = await SendAndPersistNotificationAsync(
                        receiverUserId: recipientUserId,
                        requestId: request.Id,
                        title: title,
                        body: body,
                        notificationType: notificationType,
                        userTypeId: 2 // VehicleOwner
                    );

                    // SignalR Real-time notification
                    await _signalRService.SendRequestRejectedAsync(request.Id, recipientUserId, new
                    {
                        requestId = request.Id,
                        stationOwnerName = stationOwnerName,
                        status = "rejected"
                    });

                    if (sent != null)
                        results.Add(sent);
                }

                // نفذ الحفظ مرة واحدة بعد إنهاء المعالجة
                await _db.SaveChangesAsync();

                return new ApiResponse<List<NotificationResultDto>>(results, "Charging requests processed", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<NotificationResultDto>>(null, ex.Message, false);
            }
        }
        //public async Task<ApiResponse<NotificationResultDto>> ConfirmRequestAsync(TransRequest dto)
        //{
        //    try
        //    {
        //        var userId = GetCurrentUserId();
        //        if (string.IsNullOrEmpty(userId))
        //            return new ApiResponse<NotificationResultDto>(null, "Unauthorized", false);

        //        var request = await GetAndUpdateRequestAsync(dto, RequestStatuses.Confirmed);
        //        if (request == null)
        //            return new ApiResponse<NotificationResultDto>(null, "Charging request not found", false);

        //        if (request.CarOwner?.Id != userId)
        //            return new ApiResponse<NotificationResultDto>(null, "Not your request", false);

        //        var recipientUserId = request.Charger?.User?.Id; // ChargerOwner
        //        var title = "Request Confirmed ✅";
        //        var body = $"The driver {request.CarOwner?.FullName} confirmed the charging session at your station.";
        //        var notificationType = "VehicleOwner_CompleteProcessSuccessfully";

        //        var result = await SendAndPersistNotificationAsync(
        //            receiverUserId: recipientUserId!,
        //            requestId: request.Id,
        //            title: title,
        //            body: body,
        //            notificationType: notificationType,
        //            userTypeId: 1 // ChargerOwner
        //        );


        //        return new ApiResponse<NotificationResultDto>(result, "Charging request confirmed", true);


        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<NotificationResultDto>(null, ex.Message, false);
        //    }
        //}
        public async Task<ApiResponse<NotificationResultDto>> ConfirmRequestAsync(TransRequest dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return new ApiResponse<NotificationResultDto>(null, "Unauthorized", false);

                // جلب وتحديث الطلب إلى حالة "Confirmed"
                var request = await GetAndUpdateRequestAsync(dto, RequestStatuses.Confirmed);
                if (request == null)
                    return new ApiResponse<NotificationResultDto>(null, "Charging request not found", false);

                // التأكد أن المستخدم هو الـ ChargerOwner
                if (request.Charger?.User?.Id != userId)
                    return new ApiResponse<NotificationResultDto>(null, "Not your request", false);

                // إذا تم التحقق بنجاح، إرسال الإشعار إلى الـ Vehicle Owner
                var recipientUserId = request.CarOwner?.Id; // VehicleOwner
                var title = "Charging Request Confirmed ✅";
                var body = $"The charger {request.Charger?.User?.FullName} confirmed the charging session for your vehicle.";
                var notificationType = "Charger_ConfirmedProcessSuccessfully";

                // إرسال الإشعار
                var result = await SendAndPersistNotificationAsync(
                    receiverUserId: recipientUserId!,
                    requestId: request.Id,
                    title: title,
                    body: body,
                    notificationType: notificationType,
                    userTypeId: 2 // VehicleOwner
                );

                // SignalR Real-time notification
                await _signalRService.SendRequestConfirmedAsync(request.Id, recipientUserId!, new
                {
                    requestId = request.Id,
                    chargerOwnerName = request.Charger?.User?.FullName,
                    status = "confirmed"
                });

                return new ApiResponse<NotificationResultDto>(result, "Charging request confirmed", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<NotificationResultDto>(null, ex.Message, false);
            }
        }

        public async Task<ApiResponse<NotificationResultDto>> AbortRequestAsync(TransRequest dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return new ApiResponse<NotificationResultDto>(null, "Unauthorized", false);

                // ✅ تجيب الطلب وتضبط حالته Aborted (نفس اللوجيك القديم)
                var request = await GetAndUpdateRequestAsync(dto, RequestStatuses.Aborted);
                if (request == null)
                    return new ApiResponse<NotificationResultDto>(null, "Charging request not found", false);

                // ✅ تحديد مين اللي بينفّذ الـ abort
                var carOwnerId = request.CarOwner?.Id;
                var chargerOwnerId = request.Charger?.User?.Id;

                var isVehicleOwner = carOwnerId == userId;
                var isChargerOwner = chargerOwnerId == userId;

                if (!isVehicleOwner && !isChargerOwner)
                    return new ApiResponse<NotificationResultDto>(null, "Not your request", false);

                // ✅ تجهيز بيانات الإشعار بناءً على الاتجاه
                string? recipientUserId;
                string title;
                string body;
                string notificationType;
                int recipientUserTypeId; // 1 = ChargerOwner, 2 = VehicleOwner (المستلم)

                if (isChargerOwner)
                {
                    // 🔹 صاحب الشاحن هو اللي عمل abort → تخصم منه Fees + تبلغ صاحب العربية
                    recipientUserId = carOwnerId;

                    // هنا تحط منطق خصم الرسوم من صاحب الشاحن (محفظة/رصيد/الخ...)
                    await ApplyAbortFeesForChargerOwnerAsync(request, userId);

                    title = "Charging session aborted";
                    body = "The station owner aborted your charging request.";
                    notificationType = "ChargerOwner_ProcessAborted";
                    recipientUserTypeId = 2; // VehicleOwner
                }
                else
                {
                    // 🔹 صاحب العربية هو اللي عمل abort → تبلغ صاحب الشاحن فقط
                    recipientUserId = chargerOwnerId;

                    title = "Request Aborted ❌";
                    body = $"The driver {request.CarOwner?.FullName} aborted the charging session at your station after payment.";
                    notificationType = "VehicleOwner_ProcessAbortedAfterPaymentSuccessfully";
                    recipientUserTypeId = 1; // ChargerOwner
                }

                if (string.IsNullOrEmpty(recipientUserId))
                    return new ApiResponse<NotificationResultDto>(null, "Recipient user not found", false);

                // ✅ إرسال + حفظ الإشعار
                var result = await SendAndPersistNotificationAsync(
                    receiverUserId: recipientUserId!,
                    requestId: request.Id,
                    title: title,
                    body: body,
                    notificationType: notificationType,
                    userTypeId: recipientUserTypeId
                );

                // SignalR Real-time notification
                await _signalRService.SendRequestAbortedAsync(request.Id, recipientUserId!, new
                {
                    requestId = request.Id,
                    abortedBy = isChargerOwner ? "charger_owner" : "vehicle_owner",
                    status = "aborted"
                });

                return new ApiResponse<NotificationResultDto>(result, "Charging request aborted", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<NotificationResultDto>(null, ex.Message, false);
            }
        }
        private async Task ApplyAbortFeesForChargerOwnerAsync(ChargingRequestEntity request, string chargerOwnerId)
        {
            // TODO:
            // هنا تحط منطق خصم الرسوم من صاحب الشاحن:
            // - تجيب Wallet / Balance بتاعه
            // - تحسب قيمة الـ fees حسب سياستك
            // - تخصمها وتعمل SaveChanges
            // حط لوجيك حقيقي لما تجهّز نظام الـ Wallet.
            await Task.CompletedTask;
        }

        public async Task<ApiResponse<ChargingRequestDetailsDto>> GetRequestDetailsAsync(RequestDetailsDto dto)

        {
            try
            {
                // (0) مصادقة
                var currentUserId = GetCurrentUserId();
                if (currentUserId is null)
                    return new ApiResponse<ChargingRequestDetailsDto>(null, "Unauthorized", false);

                // (1) هات الطلب
                var request = await GetRequestWithDetailsAsync(dto.RequestId);
                if (request == null)
                    return new ApiResponse<ChargingRequestDetailsDto>(null, "Charging request not found", false);
                // (2) تحقّق الملكية: صاحب المحطة فقط
                if (request.Charger?.User == null)
                    return new ApiResponse<ChargingRequestDetailsDto>(null, "Forbidden: charger owner unknown", false);

                // هات الـ UserId من التوكن كنص (استدعاء الدالة!)
                var currentUserIdRaw = GetCurrentUserIdRaw();
                if (string.IsNullOrWhiteSpace(currentUserIdRaw))
                    return new ApiResponse<ChargingRequestDetailsDto>(null, "Unauthorized", false);

                // حوّل StationOwnerId لنص للمقارنة
                var stationOwnerIdRaw = request.Charger.User.Id?.ToString();

                // السماح للـ Admin بالوصول لأي طلب
                var isAdmin = IsCurrentUserAdmin();

                if (!isAdmin && (string.IsNullOrWhiteSpace(stationOwnerIdRaw) ||
                    !string.Equals(stationOwnerIdRaw, currentUserIdRaw, StringComparison.Ordinal)))
                {
                    return new ApiResponse<ChargingRequestDetailsDto>(null, "Forbidden: not your station", false);
                }
                // (3) حساب المسافة والوقت
                double estimatedArrival = 0;
                double distanceKm = 0;
                if (request.Latitude != null && request.Longitude != null
                    && request.Charger?.Address?.Latitude != null
                    && request.Charger?.Address?.Longitude != null)
                {
                    distanceKm = CalculateDistance(
                        request.Latitude,
                        request.Longitude,
                        request.Charger.Address.Latitude,
                        request.Charger.Address.Longitude
                    );

                    double estimatedMinutes = (distanceKm / 60.0) * 60.0;
                    estimatedArrival =  Math.Ceiling(estimatedMinutes);
                }




                // (4) السعر التقديري

                // (4) السعر التقديري النهائي (Total = Base + Fee)
                decimal estimatedPriceFinal = request.EstimatedPrice;

                // fallback لو الطلب قديم/القيم صفر
                if (estimatedPriceFinal <= 0)
                {
                    // 1) حاول تستخدم BaseAmount المخزّن؛ ولو صفر احسبه من الشاحن
                    decimal baseAmount = request.BaseAmount;
                    if (baseAmount <= 0 && request.Charger?.PriceOption != null && request.Charger.Capacity?.kw > 0)
                    {
                        // حساب زمن الشحن بالساعات = الطاقة المطلوبة ÷ قدرة الشاحن
                        decimal chargingTimeInHours = (decimal)request.KwNeeded / (decimal)request.Charger.Capacity.kw;

                        // قيمة الشحن = سعر الساعة × زمن الشحن
                        baseAmount = request.Charger.PriceOption.Value * chargingTimeInHours;
                    }

                    // 2) حاول تستخدم VoltyksFee المخزّنة؛ ولو صفر احسبها من الإعدادات
                    decimal voltyksFee = request.VoltyksFees;
                    if (voltyksFee <= 0 && baseAmount > 0)
                    {
                        var feesCfg = await _feesConfigService.GetAsync();
                        voltyksFee = ApplyRules(baseAmount, feesCfg.Data.Percentage, feesCfg.Data.MinimumFee);
                    }

                    // 3) اجمع الإجمالي
                    estimatedPriceFinal = baseAmount + voltyksFee;
                }



                // (5) بيانات السيارة
                var vehicles = await _vehicleService.GetVehiclesByUserIdAsync(request.CarOwner.Id);
                var vehicle = vehicles?.Data?.FirstOrDefault();

                // (6) عنوان موقع السيارة (اختياري)
                string vehicleArea = "N/A";
                string vehicleStreet = "N/A";
                if (request.Latitude != null && request.Longitude != null)
                {
                    try
                    {
                        var (area, street) = await GetAddressFromLatLongNominatimAsync(request.Latitude, request.Longitude);
                        if (!string.IsNullOrWhiteSpace(area)) vehicleArea = area;
                        if (!string.IsNullOrWhiteSpace(street)) vehicleStreet = street;
                    }
                    catch { /* تجاهل وخلّيها N/A */ }
                }

                // (7) بناء الاستجابة
                var response = new ChargingRequestDetailsDto
                {
                    RequestId = request.Id,
                    Status = request.Status,
                    RequestedAt = request.RequestedAt,
                    CarOwnerId = request.CarOwner.Id,
                    KwNeeded = request.KwNeeded,
                    CurrentBatteryPercentage = request.CurrentBatteryPercentage,
                    CarOwnerName = $"{request.CarOwner.FirstName} {request.CarOwner.LastName}",
                    CarOwnerPhone = request.CarOwner?.PhoneNumber ?? "N/A",
                    VehicleBrand = vehicle?.BrandName ?? "Unknown",
                    VehicleModel = vehicle?.ModelName ?? "Unknown",
                    VehicleColor = vehicle?.Color ?? "Unknown",
                    VehiclePlate = vehicle?.Plate ?? "Unknown",
                    VehicleCapacity = vehicle?.Capacity ?? 0,

                    StationOwnerId = request.Charger.User.Id,
                    StationOwnerName = $"{request.Charger.User.FirstName} {request.Charger.User.LastName}",
                    ChargerId = request.ChargerId,
                    Protocol = request.Charger.Protocol?.Name ?? "Unknown",
                    CapacityKw = request.Charger.Capacity?.kw ?? 0,
                    PricePerHour = request.Charger.PriceOption?.Value ?? 0,
                    TimeNeeded = request.Charger.Capacity?.kw > 0
                        ? Math.Round(request.KwNeeded / request.Charger.Capacity.kw, 2)
                        : 0,
                    AdapterNeeded = request.Charger.Adaptor == true,
                    AdapterAvailability = request.Charger.Adaptor == true ? "Available" : "Not Available",
                    ChargerArea = request.Charger.Address?.Area ?? "N/A",
                    ChargerStreet = request.Charger.Address?.Street ?? "N/A",
                    VehicleArea = vehicleArea,
                    VehicleStreet = vehicleStreet,
                    EstimatedArrival = estimatedArrival,
                    BaseAmount = request.BaseAmount,
                    VoltyksFees = request.VoltyksFees,
                    //EstimatedPrice = request.EstimatedPrice,
                    EstimatedPrice = estimatedPriceFinal,
                    DistanceInKm = distanceKm

                };

                return new ApiResponse<ChargingRequestDetailsDto>(response, "Charging request details fetched", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ChargingRequestDetailsDto>(null, ex.Message, false);
            }
        }    
        public async Task<(string Area, string Street)> GetAddressFromLatLongNominatimAsync(double latitude, double longitude)
        {
            try
            {
                // Nominatim API (مجاني)
                string url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={latitude}&lon={longitude}&addressdetails=1&accept-language=ar";

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                // لازم User-Agent واضح (اسم مشروعك/ايميل تواصل)
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("VoltyksApp", "1.0"));
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("(support@voltyks.com)"));

                var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                    return ("N/A", "N/A");

                var body = await resp.Content.ReadAsStringAsync();
                var json = JObject.Parse(body);
                var address = json["address"] as JObject;
                if (address == null)
                    return ("N/A", "N/A");

                // نحاول نطلع الشارع
                // Nominatim ممكن يرجع street تحت مفاتيح مختلفة (road, pedestrian, footway...)
                string street =
                    (string)address["road"] ??
                    (string)address["pedestrian"] ??
                    (string)address["footway"] ??
                    (string)address["path"] ??
                    (string)address["residential"] ??
                    (string)address["neighbourhood"] ??
                    "N/A";

                // نحاول نطلع المنطقة/الحَي/المدينة
                // بنستخدم fallback ذكي حسب المتاح
                string area =
                    (string)address["suburb"] ??
                    (string)address["neighbourhood"] ??
                    (string)address["city_district"] ??
                    (string)address["city"] ??
                    (string)address["town"] ??
                    (string)address["village"] ??
                    (string)address["county"] ??
                    (string)address["state_district"] ??
                    (string)address["state"] ??
                    "N/A";

                return (area, street);
            }
            catch (HttpRequestException)
            {
                return ("N/A", "N/A");
            }
            catch (TaskCanceledException)
            {
                return ("N/A", "N/A");
            }
            catch (Exception)
            {
                return ("N/A", "N/A");
            }
        }
        public async Task<ApiResponse<decimal>> GetVoltyksFeesAsync(RequestIdDto dto, CancellationToken ct = default)
        {
            var req = await _db.Set<ChargingRequestEntity>()
                .AsNoTracking()
                .Where(r => r.Id == dto.RequestId)
                .Select(r => new { r.Id, r.VoltyksFees })
                .FirstOrDefaultAsync(ct);

            if (req is null)
                return new ApiResponse<decimal>("Request not found", status: false);

            return new ApiResponse<decimal>(req.VoltyksFees, "Fees fetched successfully", true);
        }

        public async Task<ApiResponse<object>> TransferVoltyksFeesAsync(RequestIdDto dto, CancellationToken ct = default)
        {
            // نجيب UserId, RecipientUserId, Fees (read-only to get the data for wallet update)
            var req = await _db.Set<ChargingRequestEntity>()
                .AsNoTracking()
                .Where(r => r.Id == dto.RequestId)
                .Select(r => new { r.Id, r.UserId, r.RecipientUserId, r.VoltyksFees })
                .FirstOrDefaultAsync(ct);

            if (req is null)
                return new ApiResponse<object>("Request not found", status: false);

            if (req.VoltyksFees <= 0)
                return new ApiResponse<object>("Invalid fees value", status: false,
                    new() { "VoltyksFees must be greater than zero." });

            // نجيب المستخدمين
            var ids = new[] { req.UserId, req.RecipientUserId }.Distinct().ToList();
            var users = await _db.Set<AppUser>().Where(u => ids.Contains(u.Id)).ToListAsync(ct);

            var carOwner = users.FirstOrDefault(u => u.Id == req.UserId);
            var stationOwner = users.FirstOrDefault(u => u.Id == req.RecipientUserId);

            if (carOwner is null || stationOwner is null)
                return new ApiResponse<object>("One or both users not found", status: false);

            var fees = (double)req.VoltyksFees;

            // التحويل: + لصاحب العربية، - لصاحب المحطة
            carOwner.Wallet += fees;
            stationOwner.Wallet -= fees;

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                _db.Update(carOwner);
                _db.Update(stationOwner);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new ApiResponse<object>("Failed to update wallets", status: false, errors: new() { ex.Message });
            }

            var data = new
            {
                requestId = req.Id,
                feesTransferred = req.VoltyksFees,
                userId = carOwner.Id,
                userNewWallet = carOwner.Wallet,
                recipientUserId = stationOwner.Id,
                recipientNewWallet = stationOwner.Wallet
            };
            return new ApiResponse<object>(data, "Wallets updated successfully", true);
        }
        private async Task<ChargingRequestEntity?> GetAndUpdateRequestAsync(TransRequest dto, string newStatus)
        {
            var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                            .GetAllWithIncludeAsync(
                                r => r.Id == dto.RequestId,
                                true, // trackChanges = true because we update the entity
                                r => r.Charger, r => r.Charger.User, r => r.CarOwner)) // include car owner
                            .FirstOrDefault();

            if (request == null)
                return null;

            request.Status = newStatus;
            request.RespondedAt = DateTimeHelper.GetEgyptTime();

            _unitOfWork.GetRepository<ChargingRequestEntity, int>().Update(request);
            await _unitOfWork.SaveChangesAsync();

            return request;
        }
        private async Task<Notification> AddNotificationAsync(
            string receiverUserId,
            int relatedRequestId,
            string title,
            string body,
            int userTypeId // 1 = ChargerOwner, 2 = VehicleOwner
        )
        {
            var notification = new Notification
            {
                Title = title,
                Body = body,
                IsRead = false,
                SentAt = DateTimeHelper.GetEgyptTime(),
                UserId = receiverUserId,
                RelatedRequestId = relatedRequestId,
                UserTypeId = userTypeId
            };

            await _unitOfWork.GetRepository<Notification, int>().AddAsync(notification);
            await _unitOfWork.SaveChangesAsync(); // مهم

            //await TryAutoDeleteRequestAndChildrenAsync(relatedRequestId);

            return notification;
        }
        private async Task<NotificationResultDto> SendAndPersistNotificationAsync(
        string receiverUserId,
        int requestId,
        string title,
        string body,
        string notificationType,
        int userTypeId,
        Dictionary<string, string>? extraData = null
    )
        {
            if (string.IsNullOrWhiteSpace(receiverUserId))
                throw new ArgumentException("receiverUserId is required", nameof(receiverUserId));

            var tokens = await GetDeviceTokens(receiverUserId) ?? new List<string>();

            // إرسال متوازي أسرع + أهدى
            if (tokens.Count > 0)
            {
                await System.Threading.Tasks.Task.WhenAll(tokens.Select(t =>
                    _firebaseService.SendNotificationAsync(t, title, body, requestId, notificationType, extraData)
                ));
            }

            var notification = await AddNotificationAsync(
                receiverUserId: receiverUserId,
                relatedRequestId: requestId,
                title: title,
                body: body,
                userTypeId: userTypeId
            );

            return new NotificationResultDto(
                NotificationId: notification.Id,
                RequestId: requestId,
                RecipientUserId: receiverUserId,
                Title: title,
                Body: body,
                NotificationType: notificationType,
                SentAt: notification.SentAt,
                PushSentCount: tokens.Count
            );
        }

        private string? GetCurrentUserIdRaw()
        {
            var user = _httpContext.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return null;

            // عدّل أسماء الـ claims حسب اللي عندك فعلياً
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst("uid")?.Value
                ?? user.FindFirst("user_id")?.Value
                ?? user.FindFirst("id")?.Value;
        }
        // SendChargingRequestAsync ===> Helper Private Mehtods
        private async Task<Charger?> GetChargerWithIncludes(int chargerId)
        {
            return (await _unitOfWork.GetRepository<Charger, int>()
                .GetAllWithIncludeAsync(
                    c => c.Id == chargerId && c.IsActive == true,
                    false,
                    c => c.Address,
                    c => c.Protocol,
                    c => c.Capacity,
                    c => c.PriceOption,
                    c => c.User))
                .FirstOrDefault();
        }
        private async Task<ChargingRequestEntity> CreateChargingRequest(string userId, int chargerId, double KwNeeded,int CurrentBatteryPercentage ,double Latitude,double Longitude ,string recipientUserId, Charger charger)
        {

            // 1) حساب المبلغ الأساسي (Base Amount)
            decimal baseAmount = 0m;
            if (charger?.PriceOption != null && charger.Capacity?.kw > 0)
            {
                // حساب زمن الشحن بالساعات = الطاقة المطلوبة ÷ قدرة الشاحن
                decimal chargingTimeInHours = (decimal)KwNeeded / (decimal)charger.Capacity.kw;

                // قيمة الشحن = سعر الساعة × زمن الشحن
                baseAmount = charger.PriceOption.Value * chargingTimeInHours;
            }

            // 2) حساب رسوم Voltyks
            var feesCfg = await _feesConfigService.GetAsync();
            decimal voltyksFee = ApplyRules(baseAmount, feesCfg.Data.Percentage, feesCfg.Data.MinimumFee);

            // 3) المجموع الكلي
            decimal totalEstimatedPrice = baseAmount + voltyksFee;



            var request = new ChargingRequestEntity
            {
                UserId = userId,
                ChargerId = chargerId,
                RequestedAt = DateTimeHelper.GetEgyptTime(),
                Status = "pending",
                KwNeeded = KwNeeded,
                CurrentBatteryPercentage = CurrentBatteryPercentage,
                Latitude = Latitude,
                Longitude = Longitude,
                RecipientUserId = recipientUserId,
                BaseAmount = baseAmount,
                VoltyksFees = voltyksFee,
                EstimatedPrice = totalEstimatedPrice

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
        // RegisterDeviceTokenAsync ===> Helper Private Mehtods
        private string? GetCurrentUserId()
        {
            return _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        private bool IsCurrentUserAdmin()
        {
            return _httpContext.HttpContext?.User?.IsInRole("Admin") == true;
        }
        private async Task<bool> SaveOrUpdateDeviceTokenAsync(string token, string userId)
        {
            var repo = _unitOfWork.GetRepository<DeviceToken, int>();

            // 1) امسح كل التوكنات القديمة للمستخدم
            var oldTokens = await repo.GetAllAsync(t => t.UserId == userId && t.Token != token);
            foreach (var ot in oldTokens)
                repo.Delete(ot);

            // 2) شوف لو التوكن ده موجود أصلًا
            var existing = await repo.GetFirstOrDefaultAsync(t => t.Token == token);

            if (existing != null)
            {
                // Update + ربطه بالمستخدم الحالي
                if (existing.UserId != userId)
                    existing.UserId = userId;

                existing.RegisteredAt = DateTimeHelper.GetEgyptTime();
                repo.Update(existing);
            }
            else
            {
                // Add جديد
                await repo.AddAsync(new DeviceToken
                {
                    Token = token,
                    UserId = userId,
                    RoleContext = "Owner",
                    RegisteredAt = DateTimeHelper.GetEgyptTime(),
                });
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // معالجة سباق الكتابة
                var again = await repo.GetFirstOrDefaultAsync(t => t.Token == token);
                if (again == null) throw;

                if (again.UserId != userId)
                    again.UserId = userId;

                again.RegisteredAt = DateTimeHelper.GetEgyptTime();
                repo.Update(again);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
        }
        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex?.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                return true;

            if (ex?.InnerException?.InnerException is SqlException deepSqlEx && (deepSqlEx.Number == 2601 || deepSqlEx.Number == 2627))
                return true;

            return false;
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
                    true, // AsNoTracking = true for read-only query
                    r => r.CarOwner,
                    r => r.Charger,
                    r => r.Charger.User,
                    r => r.Charger.Address,
                    r => r.Charger.Protocol,
                    r => r.Charger.Capacity,
                    r => r.Charger.PriceOption
                )).FirstOrDefault();
        }
        private static decimal ApplyRules(decimal baseAmount, decimal percentage, decimal minimumFee)
        {
            if (baseAmount < 0) baseAmount = 0;
            var pctValue = Math.Round(baseAmount * (percentage / 100m), 2, MidpointRounding.AwayFromZero);
            var result = pctValue < minimumFee ? minimumFee : pctValue;
            return Math.Round(result, 2, MidpointRounding.AwayFromZero);
        }
    }



}
