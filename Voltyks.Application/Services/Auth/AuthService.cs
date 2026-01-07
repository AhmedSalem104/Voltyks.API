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
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Complaints;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using static System.Net.WebRequestMethods;
using Voltyks.Application.Interfaces.SignalR;
using Voltyks.Core.Enums;
using Voltyks.Application.Utilities;

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
        , ISignalRService signalRService

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

            // Check if account is deleted
            if (user.IsDeleted)
            {
                return new ApiResponse<UserLoginResultDto>
                {
                    Status = false,
                    Message = "This account has been deleted. Contact support to restore."
                };
            }

            // Check if account is banned
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

            // Store refresh token with userId as key
            await redisService.SetAsync($"refresh_token:{user.Id}", refreshToken, TimeSpan.FromDays(7));
            // Store reverse mapping: token -> userId (for lookup without JWT)
            await redisService.SetAsync($"refresh_token_reverse:{refreshToken}", user.Id, TimeSpan.FromDays(7));

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
                Token = accessToken,
                RefreshToken = refreshToken
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

            // 3. Generate tokens for the new user
            var token = await GenerateJwtTokenAsync(user);
            var refreshToken = Guid.NewGuid().ToString();

            // 4. Store refresh token in Redis
            await redisService.SetAsync($"refresh_token:{user.Id}", refreshToken, TimeSpan.FromDays(7));
            await redisService.SetAsync($"refresh_token_reverse:{refreshToken}", user.Id, TimeSpan.FromDays(7));

            // 5. Return success with tokens
            return new ApiResponse<UserRegisterationResultDto>(
                new UserRegisterationResultDto
                {
                    Email = user.Email,
                    Token = token,
                    RefreshToken = refreshToken
                },
                SuccessfulMessage.UserCreatedSuccessfully
            );


        }
        public async Task<ApiResponse<TokensResponseDto>> RefreshJwtTokenAsync(RefreshTokenDto dto)
        {
            try
            {
                var request = httpContextAccessor.HttpContext.Request;
                var response = httpContextAccessor.HttpContext.Response;

                var savedRefreshToken = request.Cookies["Refresh_Token"];
                if (savedRefreshToken != dto.RefreshToken)
                {
                    return new ApiResponse<TokensResponseDto>
                    {
                        Status = false,
                        Message = ErrorMessages.InvalidOrMismatchedToken
                    };
                }

                var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext.User);
                if (user == null)
                {
                    return new ApiResponse<TokensResponseDto>
                    {
                        Status = false,
                        Message = ErrorMessages.UnauthorizedAccess
                    };
                }

                if (user.IsBanned)
                    return BannedResponse<TokensResponseDto>();

                var redisStoredToken = await redisService.GetAsync($"refresh_token:{user.Id}");
                if (redisStoredToken != dto.RefreshToken)
                {
                    return new ApiResponse<TokensResponseDto>
                    {
                        Status = false,
                        Message = ErrorMessages.RefreshTokenMismatch
                    };
                }

                var newAccessToken = await GenerateJwtTokenAsync(user);

                // Sliding Refresh Token - generate new refresh token with fresh 7-day expiry
                var newRefreshToken = Guid.NewGuid().ToString();
                var now = GetEgyptTime();
                var accessTokenExpiry = now.AddMinutes(30);
                var refreshTokenExpiry = now.AddDays(7);

                // Delete old reverse mapping and add new one
                await redisService.RemoveAsync($"refresh_token_reverse:{dto.RefreshToken}");
                await redisService.SetAsync($"refresh_token:{user.Id}", newRefreshToken, TimeSpan.FromDays(7));
                await redisService.SetAsync($"refresh_token_reverse:{newRefreshToken}", user.Id, TimeSpan.FromDays(7));

                // Update JWT Cookie
                response.Cookies.Append("JWT_Token", newAccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = accessTokenExpiry,
                    MaxAge = TimeSpan.FromMinutes(30)
                });

                // Update Refresh Token Cookie with new token
                response.Cookies.Append("Refresh_Token", newRefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = refreshTokenExpiry
                });

                // 1) جرّبه من الـ DTO لو تحب تضيف خاصية اختيارية dto.FcmToken
                var fcmFromDto = (dto as dynamic)?.FcmToken as string;

                // 2) أو من Header بدون أي API جديدة:
                var fcmFromHeader = httpContextAccessor.HttpContext?.Request?.Headers?["X-FCM-Token"].ToString();

                var fcmToken = !string.IsNullOrWhiteSpace(fcmFromDto) ? fcmFromDto : fcmFromHeader;
                if (!string.IsNullOrWhiteSpace(fcmToken))
                    await AddFcmTokenAsync(user.Id, fcmToken);

                return new ApiResponse<TokensResponseDto>(
                    data: new TokensResponseDto
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken,
                        AccessTokenExpiresAt = accessTokenExpiry,
                        RefreshTokenExpiresAt = refreshTokenExpiry
                    },
                    message: SuccessfulMessage.TokenRefreshedSuccessfully,
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokensResponseDto>
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<ApiResponse<TokensResponseDto>> RefreshJwtTokenFromCookiesAsync()
        {
            try
            {
                var request = httpContextAccessor.HttpContext.Request;
                var response = httpContextAccessor.HttpContext.Response;

                // Read refresh token from Authorization header (Bearer token)
                var authHeader = request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResponse<TokensResponseDto>
                    {
                        Status = false,
                        Message = "Refresh token not found in Authorization header. Use: Authorization: Bearer {refreshToken}"
                    };
                }

                var refreshToken = authHeader.Substring("Bearer ".Length).Trim();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return new ApiResponse<TokensResponseDto>
                    {
                        Status = false,
                        Message = "Invalid refresh token format"
                    };
                }

                // Find userId using reverse lookup in Redis (token -> userId)
                var userId = await redisService.GetAsync($"refresh_token_reverse:{refreshToken}");

                if (string.IsNullOrEmpty(userId))
                {
                    return new ApiResponse<TokensResponseDto>
                    {
                        Status = false,
                        Message = ErrorMessages.RefreshTokenMismatch
                    };
                }

                // Verify the token matches what's stored for this user
                var redisStoredToken = await redisService.GetAsync($"refresh_token:{userId}");
                if (redisStoredToken != refreshToken)
                {
                    return new ApiResponse<TokensResponseDto>
                    {
                        Status = false,
                        Message = ErrorMessages.RefreshTokenMismatch
                    };
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<TokensResponseDto>
                    {
                        Status = false,
                        Message = ErrorMessages.RefreshTokenMismatch
                    };
                }

                if (user.IsBanned)
                    return BannedResponse<TokensResponseDto>();

                var newAccessToken = await GenerateJwtTokenAsync(user);

                // Sliding Refresh Token - generate new refresh token with fresh 7-day expiry
                var newRefreshToken = Guid.NewGuid().ToString();
                var now = GetEgyptTime();
                var accessTokenExpiry = now.AddMinutes(30);
                var refreshTokenExpiry = now.AddDays(7);

                // Delete old reverse mapping and add new one
                await redisService.RemoveAsync($"refresh_token_reverse:{refreshToken}");
                await redisService.SetAsync($"refresh_token:{user.Id}", newRefreshToken, TimeSpan.FromDays(7));
                await redisService.SetAsync($"refresh_token_reverse:{newRefreshToken}", user.Id, TimeSpan.FromDays(7));

                // Update JWT Cookie
                response.Cookies.Append("JWT_Token", newAccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = accessTokenExpiry,
                    MaxAge = TimeSpan.FromMinutes(30)
                });

                // Update Refresh Token Cookie with new token
                response.Cookies.Append("Refresh_Token", newRefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = refreshTokenExpiry
                });

                // FCM token from Header
                var fcmFromHeader = httpContextAccessor.HttpContext?.Request?.Headers?["X-FCM-Token"].ToString();
                if (!string.IsNullOrWhiteSpace(fcmFromHeader))
                    await AddFcmTokenAsync(user.Id, fcmFromHeader);

                return new ApiResponse<TokensResponseDto>(
                    data: new TokensResponseDto
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken,
                        AccessTokenExpiresAt = accessTokenExpiry,
                        RefreshTokenExpiresAt = refreshTokenExpiry
                    },
                    message: SuccessfulMessage.TokenRefreshedSuccessfully,
                    status: true
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokensResponseDto>
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

            // Check ALL users including deleted ones (IsDeleted = true)
            var existingPhoneUser = await userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);

            if (existingPhoneUser is not null)
            {
                // Check if account is deleted
                if (existingPhoneUser.IsDeleted)
                    return new ApiResponse<List<string>>("Phone number belongs to a deleted account. Contact support to restore.") { Status = false };

                // Check if account is banned
                if (existingPhoneUser.IsBanned)
                    return BannedResponse<List<string>>("Phone number belongs to a banned account.");

                // Phone already exists
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

            // Check ALL users including deleted ones (IsDeleted = true)
            var existingEmailUser = await userManager.Users
                .FirstOrDefaultAsync(e => e.Email == emailDto.Email);

            if (existingEmailUser is not null)
            {
                // Check if account is deleted
                if (existingEmailUser.IsDeleted)
                    return new ApiResponse<List<string>>("Email belongs to a deleted account. Contact support to restore.") { Status = false };

                // Check if account is banned
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
        public async Task<ApiResponse<object>> GetChargerRequestsAsync(PaginationParams? paginationParams = null, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return new ApiResponse<object>(null, "Unauthorized", false);

            await CleanupOldRequestsForUserAsync(userId);

            var fiveMinutesAgo = GetEgyptTime().AddMinutes(-5);

            // Build base query using DbContext directly for pagination
            var query = context.Set<ChargingRequestEntity>()
                .AsNoTracking()
                .Include(c => c.CarOwner)
                .Include(c => c.Charger).ThenInclude(ch => ch.User)
                .Include(c => c.Charger).ThenInclude(ch => ch.Address)
                .Include(c => c.Charger).ThenInclude(ch => ch.Protocol)
                .Include(c => c.Charger).ThenInclude(ch => ch.Capacity)
                .Include(c => c.Charger).ThenInclude(ch => ch.PriceOption)
                .Where(c => c.RecipientUserId == userId
                         && c.Status == "Pending"
                         && c.RequestedAt >= fiveMinutesAgo)
                .OrderByDescending(c => c.RequestedAt);

            // Get total count for pagination
            var totalCount = await query.CountAsync(ct);

            // Apply pagination
            paginationParams ??= new PaginationParams { PageNumber = 1, PageSize = 20 };
            var skip = (paginationParams.PageNumber - 1) * paginationParams.PageSize;
            var requests = await query.Skip(skip).Take(paginationParams.PageSize).ToListAsync(ct);

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

            // Build paginated result
            var pagedResult = new PagedResult<ChargingRequestDetailsDto>(
                list,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );

            return new ApiResponse<object>(
                pagedResult,
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
            var currentWallet = user.Wallet;

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

        public async Task<ApiResponse<object>> CreateGeneralComplaintAsync(CreateGeneralComplaintDto dto, CancellationToken ct = default)
        {
            string? userId;

            // Check if UserId is provided in the request
            if (!string.IsNullOrWhiteSpace(dto.UserId))
            {
                // Use the provided UserId
                userId = dto.UserId;

                // Validate that the user exists
                var userExists = await userManager.FindByIdAsync(userId);
                if (userExists is null)
                    return new ApiResponse<object>("User not found", status: false,
                        errors: new() { "The specified UserId does not exist." });
            }
            else
            {
                // Use the currently authenticated user
                userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

                if (string.IsNullOrWhiteSpace(userId))
                    return new ApiResponse<object>("Unauthorized", status: false,
                        errors: new() { "No current user context and no UserId provided." });
            }

            // Rate limiting: 1 complaint per 6 hours
            var complaintKey = $"complaint_last:{userId}";
            var lastComplaintTime = await redisService.GetAsync(complaintKey);
            if (!string.IsNullOrEmpty(lastComplaintTime))
            {
                var lastTime = DateTime.Parse(lastComplaintTime);
                var waitTime = TimeSpan.FromHours(6) - (DateTime.UtcNow - lastTime);
                if (waitTime > TimeSpan.Zero)
                {
                    return new ApiResponse<object>(
                        $"يمكنك تقديم شكوى جديدة بعد {waitTime.Hours} ساعة و {waitTime.Minutes} دقيقة",
                        status: false,
                        errors: new() { "Rate limit exceeded. Only 1 complaint per 6 hours allowed." }
                    );
                }
            }

            // Validate category exists and not deleted
            var category = await context.ComplaintCategories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && !c.IsDeleted, ct);

            if (category is null)
                return new ApiResponse<object>("Category not found or deleted", status: false);

            // Create complaint
            var complaint = new UserGeneralComplaint
            {
                UserId = userId,
                CategoryId = dto.CategoryId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                IsResolved = false
            };

            context.UserGeneralComplaints.Add(complaint);
            await context.SaveChangesAsync(ct);

            // Record complaint time for rate limiting
            await redisService.SetAsync(complaintKey, DateTime.UtcNow.ToString("O"), TimeSpan.FromHours(6));

            // ===== Admin SignalR Notification (Real-time) =====
            var user = await userManager.FindByIdAsync(userId);
            var userName = user?.FullName ?? user?.UserName ?? "مستخدم";

            var adminNotification = new Notification
            {
                Title = "شكوى جديدة",
                Body = $"تم إنشاء شكوى جديدة من {userName}",
                IsRead = false,
                SentAt = DateTimeHelper.GetEgyptTime(),
                UserId = null,
                Type = NotificationTypes.Admin_Complaint_Created,
                OriginalId = complaint.Id,
                IsAdminNotification = true,
                UserTypeId = null
            };
            context.Notifications.Add(adminNotification);
            await context.SaveChangesAsync(ct);

            // Broadcast to Admin Dashboard via SignalR
            await signalRService.SendBroadcastAsync(
                "شكوى جديدة",
                $"تم إنشاء شكوى جديدة من {userName}",
                new
                {
                    id = $"complaint_{complaint.Id}",
                    type = "complaint",
                    originalId = complaint.Id,
                    title = "شكوى جديدة",
                    message = $"تم إنشاء شكوى جديدة من {userName}",
                    userName = userName,
                    timestamp = adminNotification.SentAt.ToString("O")
                },
                ct
            );

            return new ApiResponse<object>(
                data: new
                {
                    ComplaintId = complaint.Id,
                    CategoryId = complaint.CategoryId,
                    CategoryName = category.Name,
                    Content = complaint.Content,
                    CreatedAt = complaint.CreatedAt
                },
                message: "Complaint submitted successfully",
                status: true
            );
        }

        public async Task<ApiResponse<CanSubmitComplaintDto>> CanSubmitComplaintAsync(CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var complaintKey = $"complaint_last:{userId}";
            var lastComplaintTime = await redisService.GetAsync(complaintKey);

            if (string.IsNullOrEmpty(lastComplaintTime))
            {
                return new ApiResponse<CanSubmitComplaintDto>(
                    data: new CanSubmitComplaintDto { CanSubmit = true, HoursRemaining = 0, MinutesRemaining = 0 },
                    message: "يمكنك تقديم شكوى",
                    status: true
                );
            }

            var lastTime = DateTime.Parse(lastComplaintTime);
            var waitTime = TimeSpan.FromHours(6) - (DateTime.UtcNow - lastTime);

            if (waitTime <= TimeSpan.Zero)
            {
                return new ApiResponse<CanSubmitComplaintDto>(
                    data: new CanSubmitComplaintDto { CanSubmit = true, HoursRemaining = 0, MinutesRemaining = 0 },
                    message: "يمكنك تقديم شكوى",
                    status: true
                );
            }

            return new ApiResponse<CanSubmitComplaintDto>(
                data: new CanSubmitComplaintDto
                {
                    CanSubmit = false,
                    HoursRemaining = (int)waitTime.TotalHours,
                    MinutesRemaining = waitTime.Minutes,
                    SecondsRemaining = waitTime.Seconds
                },
                message: $"يمكنك تقديم شكوى جديدة بعد {(int)waitTime.TotalHours} ساعة و {waitTime.Minutes} دقيقة",
                status: true
            );
        }

        public async Task<ApiResponse<object>> CheckPasswordAsync(CheckPasswordDto dto, CancellationToken ct = default)
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<object>("Unauthorized", status: false);

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return new ApiResponse<object>("User not found", status: false);

            var isValid = await userManager.CheckPasswordAsync(user, dto.Password);
            if (!isValid)
                return new ApiResponse<object>("Invalid password", status: false);

            // Store password verification flag in Redis (valid for 10 minutes)
            await redisService.SetAsync($"password_verified:{userId}", "true", TimeSpan.FromMinutes(10));

            return new ApiResponse<object>(
                data: new { verified = true },
                message: "Password verified successfully",
                status: true
            );
        }

        public async Task<ApiResponse<object>> RequestEmailChangeAsync(RequestEmailChangeDto dto, CancellationToken ct = default)
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<object>("Unauthorized", status: false);

            // Check if password was verified recently
            var passwordVerified = await redisService.GetAsync($"password_verified:{userId}");
            if (string.IsNullOrEmpty(passwordVerified))
                return new ApiResponse<object>("Please verify your password first", status: false);

            // Check if new email already exists
            var existingUser = await userManager.FindByEmailAsync(dto.NewEmail);
            if (existingUser != null)
                return new ApiResponse<object>("Email already in use", status: false);

            // Generate OTP
            var otp = new Random().Next(1000, 9999).ToString();

            // Store pending email change in Redis
            var pendingData = System.Text.Json.JsonSerializer.Serialize(new { NewEmail = dto.NewEmail, Otp = otp });
            await redisService.SetAsync($"email_change:{userId}", pendingData, TimeSpan.FromMinutes(10));

            // TODO: Send OTP to new email (requires email service implementation)
            // await _emailService.SendOtpEmailAsync(dto.NewEmail, otp);

            return new ApiResponse<object>(
                data: new { message = "OTP sent to new email", otpForTesting = otp },
                message: "OTP sent successfully",
                status: true
            );
        }

        public async Task<ApiResponse<object>> VerifyEmailChangeAsync(VerifyEmailChangeDto dto, CancellationToken ct = default)
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<object>("Unauthorized", status: false);

            // Get pending email change data
            var pendingDataJson = await redisService.GetAsync($"email_change:{userId}");
            if (string.IsNullOrEmpty(pendingDataJson))
                return new ApiResponse<object>("No pending email change or request expired", status: false);

            var pendingData = System.Text.Json.JsonDocument.Parse(pendingDataJson);
            string storedEmail = pendingData.RootElement.GetProperty("NewEmail").GetString()!;
            string storedOtp = pendingData.RootElement.GetProperty("Otp").GetString()!;

            // Validate email matches
            if (!storedEmail.Equals(dto.NewEmail, StringComparison.OrdinalIgnoreCase))
                return new ApiResponse<object>("Email mismatch", status: false);

            // Validate OTP
            if (storedOtp != dto.OtpCode)
                return new ApiResponse<object>("Invalid OTP", status: false);

            // Update user email
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return new ApiResponse<object>("User not found", status: false);

            var oldEmail = user.Email;
            user.Email = dto.NewEmail;
            user.NormalizedEmail = dto.NewEmail.ToUpperInvariant();

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<object>("Failed to update email", status: false, errors: errors);
            }

            // Cleanup Redis
            await redisService.RemoveAsync($"email_change:{userId}");
            await redisService.RemoveAsync($"password_verified:{userId}");

            return new ApiResponse<object>(
                data: new { oldEmail, newEmail = dto.NewEmail },
                message: "Email changed successfully",
                status: true
            );
        }

        // ---------- Simple Change Email (No OTP) ----------
        public async Task<ApiResponse<object>> ChangeEmailAsync(ChangeEmailDto dto, CancellationToken ct = default)
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<object>("Unauthorized", status: false);

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return new ApiResponse<object>("User not found", status: false);

            // Verify current password
            var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isPasswordValid)
                return new ApiResponse<object>("Invalid password", status: false);

            // Check if new email already exists
            var existingUser = await userManager.FindByEmailAsync(dto.NewEmail);
            if (existingUser != null && existingUser.Id != userId)
                return new ApiResponse<object>("Email already in use", status: false);

            // Update email
            var oldEmail = user.Email;
            user.Email = dto.NewEmail;
            user.NormalizedEmail = dto.NewEmail.ToUpperInvariant();

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<object>("Failed to update email", status: false, errors: errors);
            }

            return new ApiResponse<object>(
                data: new { oldEmail, newEmail = dto.NewEmail },
                message: "Email changed successfully",
                status: true
            );
        }

        // ---------- Simple Change Password (No OTP) ----------
        public async Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct = default)
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new ApiResponse<object>("Unauthorized", status: false);

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return new ApiResponse<object>("User not found", status: false);

            // Change password (ChangePasswordAsync verifies current password internally)
            var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<object>("Failed to change password", status: false, errors: errors);
            }

            return new ApiResponse<object>(
                data: null,
                message: "Password changed successfully",
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
            try
            {
                // Nominatim API (مجاني)
                string url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={latitude}&lon={longitude}&addressdetails=1&accept-language=ar";

                using (var client = new HttpClient())
                {
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
