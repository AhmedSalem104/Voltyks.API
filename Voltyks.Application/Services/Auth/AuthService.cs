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
using System.Numerics;
using Voltyks.Core.DTOs;
using Voltyks.Application.Interfaces.Auth;
using Voltyks.Application.Interfaces.Redis;
using AutoMapper;

using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Core.DTOs.VehicleDTOs;
using Voltyks.Core.DTOs.Charger;


namespace Voltyks.Application.Services.Auth
{
    public class AuthService(UserManager<AppUser> userManager ,
        IHttpContextAccessor httpContextAccessor
        , IOptions<JwtOptions> options
        , IRedisService redisService
        , IConfiguration configuration
        , IMapper _mapper
        , IUnitOfWork _unitOfWork
        ) : IAuthService
    {

        public async Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(string userId)
        {
            if (!IsAuthorized(userId))
                return UnauthorizedResponse();

            var user = await GetUserWithAddressAsync(userId);
            if (user == null)
                return UserNotFoundResponse();

            var vehicles = await GetUserVehiclesAsync(userId);
            var chargers = await GetUserChargersAsync(userId);

            var result = BuildUserDetailsDto(user, vehicles, chargers);
            return new ApiResponse<UserDetailsDto>(result, SuccessfulMessage.UserDataRetrievedSuccessfully, true);
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
                    Expires = DateTime.UtcNow.AddMinutes(20),
                    MaxAge = TimeSpan.FromMinutes(20)
                });

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
            //var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(phoneNumberDto.PhoneNumber))
                return new ApiResponse<List<string>>( ErrorMessages.PhoneRequired) { Status = false };

            else if (!IsPhoneNumber(phoneNumberDto.PhoneNumber))
                return new ApiResponse<List<string>>(ErrorMessages.InvalidPhoneFormat) { Status = false };
            string normalizedPhone;
            try
            {
                normalizedPhone = NormalizePhoneToInternational(phoneNumberDto.PhoneNumber);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<string>>(ex.Message) { Status = false };
            }

            var existingPhoneUser = await userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);

            if (existingPhoneUser is not null)
                return new ApiResponse<List<string>>(ErrorMessages.PhoneAlreadyExists) { Status = false };


            return new ApiResponse<List<string>>(SuccessfulMessage.PhoneIsAvailable) { Status = true };
        }
        public async Task<ApiResponse<List<string>>> CheckEmailExistsAsync(EmailDto emailDto)
        {
            //var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(emailDto.Email))
                return new ApiResponse<List<string>>(ErrorMessages.EmailRequired) { Status = false };
            else if (!IsEmail(emailDto.Email))
                return new ApiResponse<List<string>>(ErrorMessages.InvalidEmailFormat) { Status = false };

            var existingEmailUser = await userManager.Users
                .FirstOrDefaultAsync(e => e.Email == emailDto.Email);

            if (existingEmailUser is not null)
                return new ApiResponse<List<string>>(ErrorMessages.EmailAlreadyExists) { Status = false };

          

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

        // ---------- Private Methods ----------
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
                Expires = DateTime.UtcNow.AddMinutes(jwtOptions.ExpiresInMinutes),
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
                Expires = DateTime.UtcNow.AddMinutes(20)
            });

            response.Cookies.Append("Refresh_Token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
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
        private UserDetailsDto BuildUserDetailsDto(AppUser user, List<Vehicle> vehicles, List<Charger> chargers)
        {
            var result = _mapper.Map<UserDetailsDto>(user);
            result.Vehicles = _mapper.Map<List<VehicleDto>>(vehicles);
            result.Chargers = _mapper.Map<List<ChargerDto>>(chargers);
            return result;
        }


    }
}
