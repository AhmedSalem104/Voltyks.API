using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Voltyks.Application.Interfaces;
using Voltyks.Application.Interfaces.ChargingRequest;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.ChargerRequest;
using Voltyks.Core.Enums;
using Voltyks.Infrastructure.UnitOfWork;
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

        public async Task<ApiResponse<NotificationResultDto>> SendChargingRequestAsync(SendChargingRequestDto dto)
        {
            try
            {
                var charger = await GetChargerWithIncludes(dto.ChargerId);
                if (charger == null)
                    return new ApiResponse<NotificationResultDto>(null, "Charger not found", false);

                var userId = GetCurrentUserId();
                if (userId == null)
                    return new ApiResponse<NotificationResultDto>(null, "Car owner not found", false);

                // 1) أنشئ الطلب
                var chargingRequest = await CreateChargingRequest(userId, dto.ChargerId, dto.KwNeeded, dto.CurrentBatteryPercentage , dto.Latitude,dto.Longitude);

                // 2) جهّز بيانات الإشعار
                var recipientUserId = charger.UserId; // صاحب المحطة
                var title = "New Charging Request 🚗";
                var body = $"Driver {userId} requested to charge at your station.";
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

                var result = await SendAndPersistNotificationAsync(
                    receiverUserId: recipientUserId!,
                    requestId: request.Id,
                    title: title,
                    body: body,
                    notificationType: notificationType,
                    userTypeId: 2 // VehicleOwner
                );
                await DeleteUserRequestsAndNotificationsAsync(GetCurrentUserId());


                return new ApiResponse<NotificationResultDto>(result, "Charging request accepted", true);

            }
            catch (Exception ex)
            {
                return new ApiResponse<NotificationResultDto>(null, ex.Message, false);
            }
        }
        public async Task<ApiResponse<List<NotificationResultDto>>> RejectRequestsAsync(List<TransRequest> dtos)
        {
            try
            {
                if (dtos == null || dtos.Count == 0)
                    return new ApiResponse<List<NotificationResultDto>>(null, "No requests provided", false);

                var results = new List<NotificationResultDto>();

                foreach (var dto in dtos)
                {
                    // نفس دالتك القديمة للطلب الواحد
                    var request = await GetAndUpdateRequestAsync(dto, RequestStatuses.Rejected);
                    if (request == null)
                        continue;

                    // ملاحظة: receiverUserId عندك string، فهنمشي على نفس النوع
                    var recipientUserId = request.CarOwner?.Id; // VehicleOwner
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

                    if (sent != null)
                        results.Add(sent);
                    await DeleteUserRequestsAndNotificationsAsync(GetCurrentUserId());


                }

                return new ApiResponse<List<NotificationResultDto>>(results, "Charging requests processed", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<NotificationResultDto>>(null, ex.Message, false);
            }
        }

        //public async Task<ApiResponse<NotificationResultDto>> RejectRequestAsync(TransRequest dto)
        //{
        //    try
        //    {
        //        var request = await GetAndUpdateRequestAsync(dto, RequestStatuses.Rejected);
        //        if (request == null)
        //            return new ApiResponse<NotificationResultDto>(null, "Charging request not found", false);

        //        var recipientUserId = request.CarOwner?.Id; // VehicleOwner
        //        var title = "Charging Request Rejected ❌";
        //        var body = $"Your request to charge at {request.Charger?.User?.FullName}'s station was rejected.";
        //        var notificationType = "ChargerOwner_RejectRequest";

        //        var result = await SendAndPersistNotificationAsync(
        //            receiverUserId: recipientUserId!,
        //            requestId: request.Id,
        //            title: title,
        //            body: body,
        //            notificationType: notificationType,
        //            userTypeId: 2 // VehicleOwner
        //        );
        //        return new ApiResponse<NotificationResultDto>(result, "Charging request rejected", true);


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

                var request = await GetAndUpdateRequestAsync(dto, RequestStatuses.Confirmed);
                if (request == null)
                    return new ApiResponse<NotificationResultDto>(null, "Charging request not found", false);

                if (request.CarOwner?.Id != userId)
                    return new ApiResponse<NotificationResultDto>(null, "Not your request", false);

                var recipientUserId = request.Charger?.User?.Id; // ChargerOwner
                var title = "Request Confirmed ✅";
                var body = $"The driver {request.CarOwner?.FullName} confirmed the charging session at your station.";
                var notificationType = "VehicleOwner_CompleteProcessSuccessfully";

                var result = await SendAndPersistNotificationAsync(
                    receiverUserId: recipientUserId!,
                    requestId: request.Id,
                    title: title,
                    body: body,
                    notificationType: notificationType,
                    userTypeId: 1 // ChargerOwner
                );
                await DeleteUserRequestsAndNotificationsAsync(GetCurrentUserId());

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

                var request = await GetAndUpdateRequestAsync(dto, RequestStatuses.Aborted);
                if (request == null)
                    return new ApiResponse<NotificationResultDto>(null, "Charging request not found", false);

                if (request.CarOwner?.Id != userId)
                    return new ApiResponse<NotificationResultDto>(null, "Not your request", false);

                var recipientUserId = request.Charger?.User?.Id; // ChargerOwner
                var title = "Request Aborted ❌";
                var body = $"The driver {request.CarOwner?.FullName} aborted the charging session at your station after payment.";
                var notificationType = "VehicleOwner_ProcessAbortedAfterPaymentSuccessfully";

                var result = await SendAndPersistNotificationAsync(
                    receiverUserId: recipientUserId!,
                    requestId: request.Id,
                    title: title,
                    body: body,
                    notificationType: notificationType,
                    userTypeId: 1 // ChargerOwner
                );
                await DeleteUserRequestsAndNotificationsAsync(GetCurrentUserId());

                return new ApiResponse<NotificationResultDto>(result, "Charging request aborted", true);

               
            }
            catch (Exception ex)
            {
                return new ApiResponse<NotificationResultDto>(null, ex.Message, false);
            }
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

                if (string.IsNullOrWhiteSpace(stationOwnerIdRaw) ||
                    !string.Equals(stationOwnerIdRaw, currentUserIdRaw, StringComparison.Ordinal))
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

                    double estimatedMinutes = (distanceKm / 40.0) * 60.0;
                    estimatedArrival =  Math.Ceiling(estimatedMinutes);
                }

                // (4) السعر التقديري
                decimal estimatedPrice;

                if (request.Charger?.PriceOption != null && request.Charger.Capacity?.kw > 0)
                {
                    decimal pricePerHour = request.Charger.PriceOption.Value
                                           * (decimal)request.KwNeeded
                                           / (decimal)request.Charger.Capacity.kw;

                    estimatedPrice = pricePerHour ; 
                }
                else
                {
                    estimatedPrice = 0;
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
                    PricePerHour = request.Charger.PriceOption != null ? $"{request.Charger.PriceOption.Value} EGP" : "N/A",
                    AdapterAvailability = request.Charger.Adaptor == true ? "Available" : "Not Available",
                    ChargerArea = request.Charger.Address?.Area ?? "N/A",
                    ChargerStreet = request.Charger.Address?.Street ?? "N/A",
                    VehicleArea = vehicleArea,
                    VehicleStreet = vehicleStreet,
                    EstimatedArrival = estimatedArrival,
                    EstimatedPrice = estimatedPrice,
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
            // Nominatim API (مجاني)
            string url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={latitude}&lon={longitude}&addressdetails=1&accept-language=ar";

            using (var client = new HttpClient())
            {
                // لازم User-Agent واضح (اسم مشروعك/ايميل تواصل)
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("YourAppName", "1.0"));
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("(contact@yourdomain.com)"));

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
        }
        private async Task<ChargingRequestEntity?> GetAndUpdateRequestAsync(TransRequest dto, string newStatus)
        {
            var request = (await _unitOfWork.GetRepository<ChargingRequestEntity, int>()
                            .GetAllWithIncludeAsync(
                                r => r.Id == dto.RequestId,
                                false,
                                r => r.Charger, r => r.Charger.User, r => r.CarOwner)) // include car owner
                            .FirstOrDefault();

            if (request == null)
                return null;

            request.Status = newStatus;
            request.RespondedAt = DateTime.UtcNow;

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
                SentAt = DateTime.UtcNow,
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
        int userTypeId
    )
        {
            if (string.IsNullOrWhiteSpace(receiverUserId))
                throw new ArgumentException("receiverUserId is required", nameof(receiverUserId));

            var tokens = await GetDeviceTokens(receiverUserId) ?? new List<string>();

            // إرسال متوازي أسرع + أهدى
            if (tokens.Count > 0)
            {
                await System.Threading.Tasks.Task.WhenAll(tokens.Select(t =>
                    _firebaseService.SendNotificationAsync(t, title, body, requestId, notificationType)
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
        private async Task<ChargingRequestEntity> CreateChargingRequest(string userId, int chargerId, double KwNeeded,int CurrentBatteryPercentage ,double Latitude,double Longitude)
        {
            var request = new ChargingRequestEntity
            {
                UserId = userId,
                ChargerId = chargerId,
                RequestedAt = DateTime.UtcNow,
                Status = "pending",
                KwNeeded = KwNeeded,
                CurrentBatteryPercentage = CurrentBatteryPercentage,
                Latitude = Latitude,
                Longitude = Longitude
                
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

                existing.RegisteredAt = DateTime.UtcNow;
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
                    RegisteredAt = DateTime.UtcNow,
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

                again.RegisteredAt = DateTime.UtcNow;
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

        //private async Task<bool> TryAutoDeleteRequestAndChildrenAsync(int requestId)
        //{
        //    // 1) هات الطلبات (للتأكد من الشرط)
        //    var reqRepo = _unitOfWork.GetRepository<ChargingRequestEntity, int>();
        //    var requestsToDelete = (await reqRepo.GetAllAsync(
        //            r => r.RequestedAt != null,  // نتحقق إذا كان التاريخ موجودًا
        //            trackChanges: true))
        //        .AsEnumerable()  // إيقاف الترجمة إلى SQL
        //        .Where(r => (DateTime.UtcNow - r.RequestedAt).TotalMinutes >= 5 && r.Status.ToLower() != "pending") // تنفيذ الحساب في الذاكرة
        //        .ToList();

        //    if (!requestsToDelete.Any()) return false;

        //    // 2) احفظ الـ IDs المرتبطة قبل حذف الطلب
        //    var notifRepo = _unitOfWork.GetRepository<Notification, int>();
        //    var relatedNotifIds = (await notifRepo.GetAllAsync(n => n.RelatedRequestId == requestId))
        //                          .Select(n => n.Id)
        //                          .ToList();

        //    // 3) احذف الطلبات التي لا تنطبق عليها الشروط
        //    foreach (var req in requestsToDelete)
        //    {
        //        reqRepo.Delete(req); // حذف الطلب
        //    }

        //    await _unitOfWork.SaveChangesAsync(); // حفظ التغييرات في قاعدة البيانات

        //    // 4) احذف الإشعارات المرتبطة بالطلبات المحذوفة
        //    if (relatedNotifIds.Count > 0)
        //    {
        //        var orphans = await notifRepo.GetAllAsync(n => relatedNotifIds.Contains(n.Id), trackChanges: true);
        //        foreach (var orphan in orphans)
        //        {
        //            notifRepo.Delete(orphan); // حذف الإشعارات التي تم العثور عليها
        //        }

        //        await _unitOfWork.SaveChangesAsync(); // حفظ التغييرات بعد حذف الإشعارات
        //    }

        //    return true;
        //}
        private async Task<bool> DeleteUserRequestsAndNotificationsAsync(string userId)
        {
            // 1) الحصول على الـ repository الخاص بـ ChargingRequestEntity
            var reqRepo = _unitOfWork.GetRepository<ChargingRequestEntity, int>();

            // 2) استرجاع جميع الطلبات الخاصة بالمستخدم الحالي
            var userRequests = await reqRepo.GetAllWithIncludeAsync(
                c => c.UserId == userId,  // تصفية حسب الـ userId
                false,
                c => c.Charger  // يمكنك إضافة أي علاقات إضافية إذا لزم الأمر
            );

            // 3) تصفية الطلبات حسب الشرطين: مر عليها أكثر من 5 دقائق والحالة ليست "pending"
            var requestsToDelete = userRequests
                .Where(c => (DateTime.UtcNow - c.RequestedAt).TotalMinutes >= 5
                            || c.Status.ToLower() != "pending")
                .ToList();

            // إذا لم توجد طلبات تنطبق عليها الشروط، إرجاع false
            if (!requestsToDelete.Any())
                return false;

            // 4) حذف الإشعارات المرتبطة بكل طلب
            var notifRepo = _unitOfWork.GetRepository<Notification, int>();

            // احصل على IDs الإشعارات المرتبطة بالطلب
            // الحصول على IDs الإشعارات المرتبطة بالطلبات
         
            var relatedNotifIds = (await notifRepo.GetAllAsync(n =>
                requestsToDelete.Select(r => r.Id).ToList().Contains((int)n.RelatedRequestId)))
                .Select(n => n.Id)
                .ToList();


            // حذف الإشعارات المرتبطة بالطلبات المحذوفة
            if (relatedNotifIds.Any())
            {
                var orphans = await notifRepo.GetAllAsync(n => relatedNotifIds.Contains(n.Id), trackChanges: true);
                foreach (var orphan in orphans)
                {
                    notifRepo.Delete(orphan); // حذف الإشعارات التي تم العثور عليها
                }

                await _unitOfWork.SaveChangesAsync(); // حفظ التغييرات بعد حذف الإشعارات
            }

            // 5) حذف الطلبات التي تم تصفيتها
            foreach (var request in requestsToDelete)
            {
                reqRepo.Delete(request); // حذف الطلب
            }

            // حفظ التغييرات في قاعدة البيانات بعد حذف الطلبات
            await _unitOfWork.SaveChangesAsync();

            // إرجاع true إذا تم الحذف بنجاح
            return true;
        }


    }



}
