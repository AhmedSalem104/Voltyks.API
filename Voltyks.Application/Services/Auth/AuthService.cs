using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Core.Exceptions;
using Voltyks.Core.DTOs.AuthDTOs;
using Google.Apis.Auth;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Voltyks.Persistence.Entities;
using Voltyks.Core.DTOs;
using Voltyks.Application.Interfaces.Auth;
using Voltyks.Application.Interfaces.Redis;
using AutoMapper;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Core.DTOs.VehicleDTOs;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Persistence.Data;
using ChargingRequestEntity = Voltyks.Persistence.Entities.Main.ChargingRequest;
using Voltyks.Application.Interfaces;
using Voltyks.Core.DTOs.ChargerRequest;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using static System.Net.WebRequestMethods;

namespace Voltyks.Application.Services.Auth
{
    public class AuthService(UserManager<AppUser> userManager,
        IHttpContextAccessor httpContextAccessor
        , IOptions<JwtOptions> options
        , IRedisService redisService
        , IConfiguration configuration
        , IMapper _mapper
        , IUnitOfWork _unitOfWork
        , VoltyksDbContext context
        , IVehicleService _vehicleService

        ) : IAuthService
    {

        public async Task<ApiResponse<object>> ToggleCurrentUserBanAsync(CancellationToken ct = default)
        {
            // 1) هات الـ userId من الـ Claims
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<object>("Unauthorized", status: false, errors: new() { "No current user context." });

            // 2) هات المستخدم
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return new ApiResponse<object>("User not found", status: false, errors: new() { "Invalid user." });

            // 3) Toggle
            user.IsBanned = !user.IsBanned;

            await redisService.RemoveAsync($"refresh_token:{user.Id}");
            await redisService.RemoveAsync($"fcm_tokens:{user.Id}"); // اختياري


            // اختياري: لما يتحظر ابطّل availability وامسح refresh token
            if (user.IsBanned)
            {
                user.IsAvailable = false;
                user.RefreshToken = null;
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errs = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<object>("Failed to update user", status: false, errors: errs);
            }

            var msg = user.IsBanned ? "User banned successfully" : "User unbanned successfully";
            var data = new { userId = user.Id, isBanned = user.IsBanned };

            return new ApiResponse<object>(data, message: msg, status: true);
        }
        public async Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(string userId)
        {
            if (!IsAuthorized(userId))
                return UnauthorizedResponse();

            var user = await GetUserWithAddressAsync(userId);
            if (user == null)
                return UserNotFoundResponse();

            // التحقق من حالة الحظر
            var bannedInfo = await context.UsersBanneds.FirstOrDefaultAsync(b => b.UserId == userId);
            if (bannedInfo != null)
            {
                // إذا كان الحظر ساريًا
                if (bannedInfo.BanExpiryDate.HasValue && bannedInfo.BanExpiryDate.Value > GetEgyptTime())
                {
                    return new ApiResponse<UserDetailsDto>("User is banned until " + bannedInfo.BanExpiryDate.Value.ToString("yyyy-MM-dd"), false);
                }

                // تحديث IsBanned و UserShouldBeBanned
                user.IsBanned = true;
                user.UserShouldBeBanned = true;
                context.Update(user);
                await context.SaveChangesAsync();
            }
            await HandleUserBanAsync(user);

            // استرجاع التفاصيل الإضافية للمركبات والمحطات
            var vehicles = await GetUserVehiclesAsync(userId);
            var chargers = await GetUserChargersAsync(userId);

            var result = BuildUserDetailsDto(user, vehicles, chargers);
            return new ApiResponse<UserDetailsDto>(result, SuccessfulMessage.UserDataRetrievedSuccessfully, true);
        }
        public async Task<ApiResponse<bool>> ToggleUserAvailabilityAsync()
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return new ApiResponse<bool>(ErrorMessages.UnauthorizedAccess, false);

            var userId = userIdClaim.Value;

            var user = context.Users.Find(userId);


            if (user == null)
                return new ApiResponse<bool>("User not found", false);


            user.IsAvailable = !user.IsAvailable;
            await _unitOfWork.SaveChangesAsync();

            var newStatus = user.IsAvailable ? "Available" : "Unavailable";

            return new ApiResponse<bool>(
                data: user.IsAvailable,
                message: $"User availability changed to: {newStatus}",
                status: true
            );
        }
        public async Task<ApiResponse<UserLoginResultDto>> LoginAsync(LoginDTO model)
        {
            var user = await GetUserByUsernameOrPhoneAsync(model.EmailOrPhone);

            if (user == null)
            {
                return new ApiResponse<UserLoginResultDto>
                {
                    Status = false,
                    Message = ErrorMessages.UserNotFound
                };
            }
            // 🚫 جديد: منع دخول المحظورين
            if (user.IsBanned)
                return BannedResponse<UserLoginResultDto>();

            var isPasswordValid = await userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                return new ApiResponse<UserLoginResultDto>
                {
                    Status = false,
                    Message = ErrorMessages.InvalidPasswordOrEmailAddress
                };
            }

            var accessToken = await GenerateJwtTokenAsync(user);
            var refreshToken = Guid.NewGuid().ToString();

            await redisService.SetAsync($"refresh_token:{user.Id}", refreshToken, TimeSpan.FromDays(7));

            SetCookies(accessToken, refreshToken);

            var userWithAddress = userManager.Users
                .Include(u => u.Address)
                .FirstOrDefault(u => u.Id == user.Id);

            var result = new UserLoginResultDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                City = userWithAddress?.Address?.City,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Token = accessToken
            };

            return new ApiResponse<UserLoginResultDto>(result, SuccessfulMessage.LoginSuccessful);
        }
        public async Task<ApiResponse<UserRegisterationResultDto>> RegisterAsync(RegisterDTO model)
        {
            var validationContext = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            if (!isValid)
            {
                var errors = validationResults.Select(r => r.ErrorMessage).ToList();
                return new ApiResponse<UserRegisterationResultDto>
                {
                    Status = false,
                    Message = "Validation failed",
                    Data = null,
                    Errors = errors
                };
            }
            string normalizedPhone;
            try
            {
                normalizedPhone = NormalizePhoneToInternational(model.PhoneNumber);
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserRegisterationResultDto>
                {
                    Status = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            model.PhoneNumber = normalizedPhone;


            // 🚫 جديد: لو فيه حساب محظور بنفس الإيميل → امنع التسجيل
            var bannedByEmail = await userManager.Users
                .AnyAsync(u => u.Email == model.Email && u.IsBanned);
            if (bannedByEmail)
                return BannedResponse<UserRegisterationResultDto>("Email is associated with a banned account.");

            // 🚫 جديد: لو فيه حساب محظور بنفس رقم الموبايل → امنع التسجيل
            var bannedByPhone = await userManager.Users
                .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.IsBanned);
            if (bannedByPhone)
                return BannedResponse<UserRegisterationResultDto>("Phone number is associated with a banned account.");


            var emailCheck = await CheckEmailExistsAsync(new EmailDto { Email = model.Email });
            if (!emailCheck.Status)
            {
                return new ApiResponse<UserRegisterationResultDto>
                {
                    Status = false,
                    Message = ErrorMessages.EmailAlreadyExists,
                    Errors = emailCheck.Errors ?? new List<string> { emailCheck.Message }
                };
            }

            var phoneCheck = await CheckPhoneNumberExistsAsync(new PhoneNumberDto { PhoneNumber = model.PhoneNumber });
            if (!phoneCheck.Status)
            {
                return new ApiResponse<UserRegisterationResultDto>
                {
                    Status = false,
                    Message = ErrorMessages.PhoneAlreadyExists,
                    Errors = phoneCheck.Errors ?? new List<string> { phoneCheck.Message }
                };
            }

            var user = CreateUserFromRegisterModel(model);

            // 1. Call CreateUserAsync and store the result
            var creationResult = await CreateUserAsync(user, model.Password);

            // 2. Check if user creation failed
            if (!creationResult.Status)
            {
                return new ApiResponse<UserRegisterationResultDto>
                {
                    Status = false,
                    Message = creationResult.Message,
                    Errors = creationResult.Data
                };
            }

            // 3. Only here if creation succeeded
            return new ApiResponse<UserRegisterationResultDto>(
                MapToUserResultDto(user),
               SuccessfulMessage.UserCreatedSuccessfully
            );


        }
        public async Task<ApiResponse<string>> RefreshJwtTokenAsync(RefreshTokenDto dto)
        {
            try
            {
                var request = httpContextAccessor.HttpContext.Request;
                var response = httpContextAccessor.HttpContext.Response;

                var savedRefreshToken = request.Cookies["Refresh_Token"];
                if (savedRefreshToken != dto.RefreshToken)
                {
                    return new ApiResponse<string>
                    {
                        Status = false,
                        Message = ErrorMessages.InvalidOrMismatchedToken
                    };
                }

                var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext.User);
                if (user == null)
                {
                    return new ApiResponse<string>
                    {
                        Status = false,
                        Message = ErrorMessages.UnauthorizedAccess
                    };
                }

                if (user.IsBanned)
                    return BannedResponse<string>();

                var redisStoredToken = await redisService.GetAsync($"refresh_token:{user.Id}");
                if (redisStoredToken != dto.RefreshToken)
                {
                    return new ApiResponse<string>
                    {
                        Status = false,
                        Message = ErrorMessages.RefreshTokenMismatch
                    };
                }

                var newAccessToken = await GenerateJwtTokenAsync(user);

                response.Cookies.Append("JWT_Token", newAccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = GetEgyptTime().AddMinutes(20),
                    MaxAge = TimeSpan.FromMinutes(20)
                });

                // 1) جرّبه من الـ DTO لو تحب تضيف خاصية اختيارية dto.FcmToken
                var fcmFromDto = (dto as dynamic)?.FcmToken as string;

                // 2) أو من Header بدون أي API جديدة:
                var fcmFromHeader = httpContextAccessor.HttpContext?.Request?.Headers?["X-FCM-Token"].ToString();

                var fcmToken = !string.IsNullOrWhiteSpace(fcmFromDto) ? fcmFromDto : fcmFromHeader;
                if (!string.IsNullOrWhiteSpace(fcmToken))
                    await AddFcmTokenAsync(user.Id, fcmToken);

                return new ApiResponse<string>(
                    data: newAccessToken,
                    message: SuccessfulMessage.TokenRefreshedSuccessfully,
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<ApiResponse<List<string>>> CheckPhoneNumberExistsAsync(PhoneNumberDto phoneNumberDto)
        {
            if (string.IsNullOrWhiteSpace(phoneNumberDto.PhoneNumber))
                return new ApiResponse<List<string>>(ErrorMessages.PhoneRequired) { Status = false };
            else if (!IsPhoneNumber(phoneNumberDto.PhoneNumber))
                return new ApiResponse<List<string>>(ErrorMessages.InvalidPhoneFormat) { Status = false };

            string normalizedPhone;
            try { normalizedPhone = NormalizePhoneToInternational(phoneNumberDto.PhoneNumber); }
            catch (Exception ex) { return new ApiResponse<List<string>>(ex.Message) { Status = false }; }

            var existingPhoneUser = await userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);

            if (existingPhoneUser is not null)
            {
                // 🚫 جديد: لو الحساب محظور
                if (existingPhoneUser.IsBanned)
                    return BannedResponse<List<string>>("Phone number belongs to a banned account.");

                // كان عندك قبل كده “موجود”
                return new ApiResponse<List<string>>(ErrorMessages.PhoneAlreadyExists) { Status = false };
            }

            return new ApiResponse<List<string>>(SuccessfulMessage.PhoneIsAvailable) { Status = true };
        }
        public async Task<ApiResponse<List<string>>> CheckEmailExistsAsync(EmailDto emailDto)
        {
            if (string.IsNullOrWhiteSpace(emailDto.Email))
                return new ApiResponse<List<string>>(ErrorMessages.EmailRequired) { Status = false };
            else if (!IsEmail(emailDto.Email))
                return new ApiResponse<List<string>>(ErrorMessages.InvalidEmailFormat) { Status = false };

            var existingEmailUser = await userManager.Users
                .FirstOrDefaultAsync(e => e.Email == emailDto.Email);

            if (existingEmailUser is not null)
            {
                // 🚫 جديد: لو الحساب محظور
                if (existingEmailUser.IsBanned)
                    return BannedResponse<List<string>>("Email belongs to a banned account.");

                return new ApiResponse<List<string>>(ErrorMessages.EmailAlreadyExists) { Status = false };
            }

            return new ApiResponse<List<string>>(null, SuccessfulMessage.EmailIsAvailable) { Status = true };
        }
        public async Task<ApiResponse<List<string>>> LogoutAsync(TokenDto dto)
        {
            var tokenFromCookies = httpContextAccessor.HttpContext.Request.Cookies["JWT_Token"];

            // التحقق من تطابق الـ token في الكوكيز مع الـ token المرسل
            if (string.IsNullOrEmpty(tokenFromCookies) || tokenFromCookies != dto.Token)
            {
                return new ApiResponse<List<string>>(ErrorMessages.InvalidOrMismatchedToken) { Status = false };

            }

            // إزالة الجلسة من Redis
            await redisService.RemoveAsync($"session:{dto.Token}");

            // حذف الكوكيز
            var response = httpContextAccessor.HttpContext.Response;
            response.Cookies.Delete("JWT_Token");
            response.Cookies.Delete("Refresh_Token");

            return new ApiResponse<List<string>>(SuccessfulMessage.LoggedOutSuccessfully) { Status = true };

        }
        public async Task<ApiResponse<List<ChargingRequestDetailsDto>>> GetChargerRequestsAsync()
        {

            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return new ApiResponse<List<ChargingRequestDetailsDto>>(
                    null, "Unauthorized", false);


            await CleanupOldRequestsForUserAsync(userId);


            var repo = _unitOfWork.GetRepository<ChargingRequestEntity, int>();
            // ✅ نضيف الشرط هنا
            var fiveMinutesAgo = GetEgyptTime().AddMinutes(-5);

            var requests = (await repo.GetAllWithIncludeAsync(
                c => c.RecipientUserId == userId
                     && c.Status == "Pending"
                     && c.RequestedAt >= fiveMinutesAgo,
                false,
                c => c.CarOwner,
                c => c.Charger,
                c => c.Charger.User,
                c => c.Charger.Address,
                c => c.Charger.Protocol,
                c => c.Charger.Capacity,
                c => c.Charger.PriceOption
            )).ToList();

            var list = _mapper.Map<List<ChargingRequestDetailsDto>>(requests);

            for (int i = 0; i < list.Count; i++)
            {
                var req = requests[i];
                var dto = list[i];

                // حساب المسافة والوقت التقريبي
                if (req.Latitude != 0 && req.Longitude != 0
                    && req.Charger?.Address?.Latitude != 0
                    && req.Charger?.Address?.Longitude != 0)
                {
                    dto.DistanceInKm = CalculateDistance(
                        req.Latitude,
                        req.Longitude,
                        req.Charger.Address.Latitude,
                        req.Charger.Address.Longitude
                    );

                    // افتراض سرعة 40 كم/س → وقت الوصول بالدقائق
                    dto.EstimatedArrival = Math.Ceiling((dto.DistanceInKm / 60.0) * 60.0);
                }

                // تقدير السعر
                if (req.Charger?.PriceOption != null && req.Charger.Capacity?.kw > 0)
                {
                    dto.EstimatedPrice = req.Charger.PriceOption.Value
                                         * (decimal)req.KwNeeded
                                         / (decimal)req.Charger.Capacity.kw;
                }
                // (6) عنوان موقع السيارة (اختياري)
                string vehicleArea = "N/A";
                string vehicleStreet = "N/A";
                if (req.Latitude != null && req.Longitude != null)
                {
                    try
                    {
                        var (area, street) = await GetAddressFromLatLongNominatimAsync(req.Latitude, req.Longitude);
                        if (!string.IsNullOrWhiteSpace(area)) vehicleArea = area;
                        if (!string.IsNullOrWhiteSpace(street)) vehicleStreet = street;
                        dto.VehicleArea = vehicleArea;
                        dto.VehicleStreet = vehicleStreet;
                    }
                    catch { /* تجاهل وخلّيها N/A */ }
                }
                // بيانات السيارة (أول سيارة للمستخدم صاحب الطلب)
                var vehicles = await _vehicleService.GetVehiclesByUserIdAsync(req.CarOwner.Id);
                var vehicle = vehicles?.Data?.FirstOrDefault();
                if (vehicle != null)
                {
                    dto.VehicleBrand = vehicle.BrandName;
                    dto.VehicleModel = vehicle.ModelName;
                    dto.VehicleColor = vehicle.Color;
                    dto.VehiclePlate = vehicle.Plate;
                    dto.VehicleCapacity = vehicle.Capacity;
                }


            }

            return new ApiResponse<List<ChargingRequestDetailsDto>>(
                list,
                SuccessfulMessage.DataRetrievedSuccessfully,
                true
            );
        }
        public async Task<ApiResponse<double?>> GetMyWalletAsync(CancellationToken ct = default)
        {
            var userId =
                httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<double?>("Unauthorized", status: false, errors: new() { "No current user context." });

            // أسرع استعلام مباشر لقيمة الـ Wallet
            var wallet = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Wallet)   // double?
                .FirstOrDefaultAsync(ct);

            // لو مش لاقي مستخدم (نادرًا)
            if (wallet == default && !await userManager.Users.AnyAsync(u => u.Id == userId, ct))
                return new ApiResponse<double?>("User not found", status: false);

            return new ApiResponse<double?>(
                data: wallet,                    // ممكن تكون null لو لسه ما اتحددتش
                message: "Wallet fetched successfully",
                status: true
            );
        }

        public async Task<ApiResponse<double?>> ResetMyWalletAsync(CancellationToken ct = default)
        {
            var userId =
                httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<double?>("Unauthorized", status: false,
                    errors: new() { "No current user context." });

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return new ApiResponse<double?>("User not found", status: false);

            user.Wallet = 0;
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errs = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<double?>("Failed to reset wallet", status: false, errors: errs);
            }

            return new ApiResponse<double?>(
                data: 0,
                message: "Wallet reset to 0 successfully",
                status: true
            );
        }

        public async Task<ApiResponse<object>> DeductFeesFromWalletAsync(int requestId, CancellationToken ct = default)
        {
            // 1. Get the charging request
            var chargingRequest = await context.Set<ChargingRequestEntity>()
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (chargingRequest is null)
                return new ApiResponse<object>("Charging request not found", status: false);

            // 2. Get the user (car owner)
            var user = await userManager.FindByIdAsync(chargingRequest.UserId);
            if (user is null)
                return new ApiResponse<object>("User not found", status: false);

            // 3. Get the fees
            var fees = (double)chargingRequest.VoltyksFees;
            var currentWallet = user.Wallet ?? 0;

            // 4. Calculate the deduction
            double deductedAmount;
            double newWallet;

            if (currentWallet >= fees)
            {
                // Wallet has enough - deduct only the fees
                deductedAmount = fees;
                newWallet = currentWallet - fees;
            }
            else
            {
                // Wallet doesn't have enough - deduct all available (reset to 0)
                deductedAmount = currentWallet;
                newWallet = 0;
            }

            // 5. Update the wallet
            user.Wallet = newWallet;
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errs = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<object>("Failed to update wallet", status: false, errors: errs);
            }

            return new ApiResponse<object>(
                data: new
                {
                    RequestId = requestId,
                    UserId = user.Id,
                    UserName = user.FullName,
                    FeesAmount = fees,
                    PreviousWallet = currentWallet,
                    DeductedAmount = deductedAmount,
                    NewWallet = newWallet,
                    IsFullyDeducted = deductedAmount >= fees
                },
                message: currentWallet >= fees
                    ? "Fees deducted successfully"
                    : $"Partial deduction: Only {deductedAmount:F2} deducted (wallet insufficient)",
                status: true
            );
        }

        // ---------- Private Methods ----------
        private async Task CleanupOldRequestsForUserAsync(string userId)
        {
            var cutoff = GetEgyptTime().AddMinutes(-5);

            var reqRepo = _unitOfWork.GetRepository<ChargingRequestEntity, int>();
            var notifRepo = _unitOfWork.GetRepository<Notification, int>();

            // الطلبات المطلوب حذفها للمستخدم (سواء كان هو صاحب الطلب أو المستلم)
            var toDelete = (await reqRepo.GetAllAsync(
                c =>
                    (c.UserId == userId || c.RecipientUserId == userId) &&
                    (
                        // Pending أقدم من 5 دقائق
                        ((c.Status == "pending" || c.Status == "Pending") && c.RequestedAt <= cutoff)
                        // أو مرفوض
                        || (c.Status == "rejected" || c.Status == "Rejected")
                    ),
                trackChanges: true   // مهم علشان نقدر نحذف
            )).ToList();

            if (toDelete.Count == 0) return;

            var ids = toDelete.Select(r => r.Id).ToList();

            // احذف الإشعارات المرتبطة أولاً (لو مفيش Cascade)
            var related = (await notifRepo.GetAllAsync(
                n => n.RelatedRequestId.HasValue && ids.Contains(n.RelatedRequestId.Value),
                trackChanges: true
            )).ToList();

            foreach (var n in related) notifRepo.Delete(n);
            foreach (var r in toDelete) reqRepo.Delete(r);

            await _unitOfWork.SaveChangesAsync();
        }

        private string GetCurrentUserId()
        {
            return httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new UnauthorizedAccessException(ErrorMessages.UserNotAuthenticated);
        }
        private bool IsPhoneNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var phonePattern = @"^(?:\+20|0020|0)?1[0125]\d{8}$";
            return Regex.IsMatch(input, phonePattern);
        }
        private bool IsEmail(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(input);
                return addr.Address == input;
            }
            catch
            {
                return false;
            }
        }
        private string NormalizePhoneNumber(string phone)
        {
            if (phone.StartsWith("+20"))
                phone = phone.Replace("+20", "0");
            else if (phone.StartsWith("0020"))
                phone = phone.Replace("0020", "0");

            return phone;
        }
        private async Task<FacebookUserDto> VerifyExternalToken(ExternalAuthDto dto)
        {
            if (dto.Provider.ToLower() == "google")
            {
                var googleClientId = configuration["Authentication:Google:client_id"];

                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { googleClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);

                return new FacebookUserDto
                {
                    Email = payload.Email,
                    Name = payload.Name
                };
            }
            else if (dto.Provider.ToLower() == "facebook")
            {
                using var http = new HttpClient();
                var response = await http.GetAsync($"https://graph.facebook.com/me?access_token={dto.IdToken}&fields=id,name,email");

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                dynamic fbData = JsonConvert.DeserializeObject(content);

                return new FacebookUserDto
                {
                    Email = fbData.email,
                    Name = fbData.name
                };
            }

            return null;
        }
        private async Task<string> GenerateJwtTokenAsync(AppUser user)
        {
            var jwtOptions = options.Value;
            // إعداد Claims
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("DisplayName", user.FullName ?? "")
        };


            // إضافة الأدوار كـ Claims
            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // مفتاح التوقيع
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey));
            // التوقيع
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // إعداد الـ Token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = GetEgyptTime().AddMinutes(jwtOptions.ExpiresInMinutes),
                SigningCredentials = creds,
                Issuer = jwtOptions.Issuer,
                Audience = jwtOptions.Audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
        private async Task<AppUser?> GetUserByUsernameOrPhoneAsync(string usernameOrPhone)
        {
            if (usernameOrPhone.Contains("@"))
            {
                var emailUser = await userManager.FindByEmailAsync(usernameOrPhone);
                if (emailUser == null)
                    throw new UnAuthorizedException("Email does not exist");

                if (string.IsNullOrEmpty(emailUser.PhoneNumber))
                    throw new UnAuthorizedException("There is no phone number associated with this mail");

                return await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == emailUser.PhoneNumber);
            }
            else
            {
                var normalizedPhone = NormalizePhoneToInternational(usernameOrPhone);
                return await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            }
        }
        private void SetCookies(string accessToken, string refreshToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            response.Cookies.Append("JWT_Token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = GetEgyptTime().AddMinutes(20)
            });

            response.Cookies.Append("Refresh_Token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = GetEgyptTime().AddDays(7)
            });
        }
        private AppUser CreateUserFromRegisterModel(RegisterDTO model)
        {
            return new AppUser
            {
                UserName = model.FirstName.Replace(" ", "") + model.LastName.Replace(" ", "") + Guid.NewGuid().ToString("N").Substring(0, 8),
                //UserName = model.FirstName + model.LastName + Guid.NewGuid().ToString("N").Substring(0, 8),
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = new Persistence.Entities.Identity.Address
                {

                    City = model.City,        // ديناميكي من DTO
                    Country = "Egypt"         // قيمة ثابتة
                }
            };
        }
        private async Task<ApiResponse<List<string>>> CreateUserAsync(AppUser user, string password)
        {
            var result = await userManager.CreateAsync(user, password);
            //await _unitOfWork.SaveChangesAsync();

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<List<string>>(errors, ErrorMessages.UserCreationFailed) { Status = false };
            }

            return new ApiResponse<List<string>>(null, SuccessfulMessage.UserCreatedSuccessfully) { Status = true };
        }
        private UserRegisterationResultDto MapToUserResultDto(AppUser user)
        {
            return new UserRegisterationResultDto
            {
                Email = user.Email
            };
        }
        private string NormalizePhoneToInternational(string phone)
        {


            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone number is required.");



            // الشكل المحلي 010xxxxxxxx
            if (Regex.IsMatch(phone, @"^01[0-9]{9}$"))
            {
                return $"+2{phone}";  // نحوله إلى الصيغة الدولية
            }

            // الشكل الدولي +2010xxxxxxxx
            if (Regex.IsMatch(phone, @"^\+201[0-9]{9}$"))
            {
                return phone; // مقبول بالفعل
            }

            throw new ArgumentException("Invalid phone format. Accepted formats: 010xxxxxxxx or +2010xxxxxxxx.");


        }
        private bool IsAuthorized(string requestedUserId)
        {
            var currentUserId = GetCurrentUserId();
            return currentUserId == requestedUserId;
        }
        private ApiResponse<UserDetailsDto> UnauthorizedResponse()
        {
            return new ApiResponse<UserDetailsDto>("Unauthorized access", false, new List<string> { "You are not allowed to view this data." });
        }
        private async Task<AppUser?> GetUserWithAddressAsync(string userId)
        {
            return await userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        private ApiResponse<UserDetailsDto> UserNotFoundResponse()
        {
            return new ApiResponse<UserDetailsDto>("User not found", false);
        }
        private async Task<List<Vehicle>> GetUserVehiclesAsync(string userId)
        {
            var vehicleRepo = _unitOfWork.GetRepository<Vehicle, int>();
            return (await vehicleRepo.GetAllWithIncludeAsync(
             v => v.UserId == userId && !v.IsDeleted,
             false,
             v => v.Brand,
             v => v.Model
          )).ToList();

        }
        private async Task<List<Charger>> GetUserChargersAsync(string userId)
        {
            var chargerRepo = _unitOfWork.GetRepository<Charger, int>();
            return (await chargerRepo.GetAllWithIncludeAsync(
                 c => c.UserId == userId && !c.IsDeleted,
                 false,
                 c => c.Capacity,
                 c => c.Protocol,
                 c => c.Address,
                 c => c.PriceOption
             )).ToList();

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
        private UserDetailsDto BuildUserDetailsDto(AppUser user, List<Vehicle> vehicles, List<Charger> chargers)
        {
            var result = _mapper.Map<UserDetailsDto>(user);

            result.Vehicles = _mapper.Map<List<VehicleDto>>(vehicles);
            result.Chargers = _mapper.Map<List<ChargerDto>>(chargers);

            return result;
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
        private static ApiResponse<T> BannedResponse<T>(string? message = null)
        {
            var msg = message ?? "user_Is_Banned";
            return new ApiResponse<T>(msg, status: false, errors: new List<string> { msg });
        }
        private async Task HandleUserBanAsync(AppUser user)
        {
            // التحقق إذا كان المستخدم محظورًا في جدول UsersBanned
            var bannedInfo = await context.UsersBanneds.FirstOrDefaultAsync(b => b.UserId == user.Id);

            if (bannedInfo != null)
            {
                // إذا كان الحظر ساريًا
                if (bannedInfo.BanExpiryDate.HasValue && bannedInfo.BanExpiryDate.Value > GetEgyptTime())
                {
                    // إرجاع رسالة تفيد أن المستخدم محظور حتى تاريخ معين
                    throw new InvalidOperationException($"User is banned until {bannedInfo.BanExpiryDate.Value.ToString("yyyy-MM-dd")}");
                }

                // إذا انتهت فترة الحظر، نقوم بتحديث حالة الحظر
                user.IsBanned = true;
                user.UserShouldBeBanned = true;
                context.Update(user);
                await context.SaveChangesAsync();
            }
        }
        public static DateTime GetEgyptTime()
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptZone);
        }



        private const int FcmTokenTtlDays = 30;
        private string BuildFcmRedisKey(string userId) => $"fcm_tokens:{userId}";
        private static HashSet<string> ParseCsv(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new();
            return new(csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        private static string ToCsv(HashSet<string> set) => string.Join(',', set);
        private async Task AddFcmTokenAsync(string userId, string fcmToken)
        {
            var key = BuildFcmRedisKey(userId);
            var csv = await redisService.GetAsync(key);
            var set = ParseCsv(csv);
            if (set.Add(fcmToken) || true) // نجدد TTL حتى لو موجود
                await redisService.SetAsync(key, ToCsv(set), TimeSpan.FromDays(FcmTokenTtlDays));
        }
        private async Task RemoveFcmTokenAsync(string userId, string fcmToken)
        {
            var key = BuildFcmRedisKey(userId);
            var csv = await redisService.GetAsync(key);
            var set = ParseCsv(csv);
            if (set.Remove(fcmToken))
                if (set.Count == 0) await redisService.RemoveAsync(key);
                else await redisService.SetAsync(key, ToCsv(set), TimeSpan.FromDays(FcmTokenTtlDays));
        }
        private async Task<List<string>> GetFcmTokensAsync(string userId)
        {
            var key = BuildFcmRedisKey(userId);
            var csv = await redisService.GetAsync(key);
            return ParseCsv(csv).ToList();
        }

    }
}
