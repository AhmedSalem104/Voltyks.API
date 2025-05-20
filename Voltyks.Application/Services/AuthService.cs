using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using V_Exception = Voltyks.Core.Exceptions;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Core.Exceptions;
using Voltyks.Application.Interfaces;
using System.Security.Cryptography;
using Voltyks.Core.DTOs.AuthDTOs;
using Google.Apis.Auth;
using Newtonsoft.Json;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Voltyks.Persistence.Entities.Main;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Voltyks.Core.DTOs;

namespace Voltyks.Application
{
    public class AuthService(UserManager<AppUser> userManager ,
        IHttpContextAccessor httpContextAccessor
        , IOptions<JwtOptions> options
        , IOptions<TwilioSettings> twilioSettings
        , IRedisService redisService
        , IConfiguration configuration ) : IAuthService
    {


        // دالة تسجيل الدخول بالايميل او رقم التليفون
        public async Task<UserLoginResultDto> LoginAsync(LoginDTO model)
        {
            var user = await GetUserByUsernameOrPhoneAsync(model.UsernameOrPhone);

            if (user == null)
                throw new UnAuthorizedException("User does not exist");

            var isPasswordValid = await userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
                throw new UnAuthorizedException("Invalid password or email address");

            var accessToken = await GenerateJwtTokenAsync(user);
            var refreshToken = Guid.NewGuid().ToString();

            await redisService.SetAsync($"refresh_token:{user.Id}", refreshToken, TimeSpan.FromDays(7));

            SetCookies(accessToken, refreshToken);

            var userWithAddress = userManager.Users
                                .Include(u => u.Address)  // هنا بنعمل Include للـ Address المرتبط
                                .FirstOrDefault(u => u.Id == user.Id);

            return new UserLoginResultDto()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                City = userWithAddress.Address?.City,  
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Token = accessToken
            };

        }

        // دالة تسجيل مستخدم جديد
        public async Task<UserRegisterationResultDto> RegisterAsync(RegisterDTO model)
        {
            // تحقق من صحة الـ DataAnnotations
            var validationContext = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            if (!isValid)
            {
                var errors = validationResults.Select(r => r.ErrorMessage).ToList();
                throw new V_Exception.ValidationException(errors);
            }

            // ✅ تحقق من تنسيق رقم الهاتف
            if (!Regex.IsMatch(model.PhoneNumber ?? "", @"^\+20\d{10}$"))
            {
                throw new V_Exception.ValidationException(new[] { "Phone number must start with '+20' followed by 10 digits." });
            }


            // ✅ تحقق من عدم تكرار الإيميل والرقم
            await CheckEmailExistsAsync(new EmailDto { Email = model.Email });
            await CheckPhoneNumberExistsAsync(new PhoneNumberDto { PhoneNumber = model.PhoneNumber });

            // إنشاء المستخدم
            var user = CreateUserFromRegisterModel(model);
            await CreateUserAsync(user, model.Password);

            return MapToUserResultDto(user);
        }


        public async Task<string> RefreshJwtTokenAsync(RefreshTokenDto dto)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var response = httpContextAccessor.HttpContext.Response;

            var savedRefreshToken = request.Cookies["Refresh_Token"];
            if (savedRefreshToken != dto.RefreshToken)
                throw new SecurityTokenExpiredException("Refresh token is invalid or expired");

            var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext.User);
            if (user == null)
                throw new SecurityTokenExpiredException();

            var redisStoredToken = await redisService.GetAsync($"refresh_token:{user.Id}");
            if (redisStoredToken != dto.RefreshToken)
                throw new UnAuthorizedException("Refresh token mismatch");

            var newAccessToken = await GenerateJwtTokenAsync(user);

            response.Cookies.Append("JWT_Token", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(20),
                MaxAge = TimeSpan.FromMinutes(20)
            });

            return newAccessToken;
        }


        // دالة ارسال  ال OTP 
        public async Task SendOtpAsync(PhoneNumberDto phoneNumberDto)
        {
            var phoneNumber = phoneNumberDto.PhoneNumber;
            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            await redisService.SetAsync($"otp:{phoneNumber}", otpCode, TimeSpan.FromMinutes(5));
            Console.WriteLine($"OTP sent to {phoneNumber}: {otpCode}");
        }

        // دالة التحقق من ال OTP المرسل
        public async Task<bool> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var cachedOtp = await redisService.GetAsync($"otp:{dto.PhoneNumber}");
            if (cachedOtp == null || cachedOtp != dto.OtpCode)
                return false;

            await redisService.RemoveAsync($"otp:{dto.PhoneNumber}");
            return true;
        }

        // دالة  نسيت كلمة المرور
        public async Task ForgotPasswordAsync(PhoneNumberDto phoneNumberDto)
        {
            var phoneNumber = phoneNumberDto.PhoneNumber;
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user == null)
                throw new NotFoundException("User not found");

            await SendOtpAsync(phoneNumberDto); // استدعاء الدالة المعدّلة
        }

        // دالة  تغيير كلمة المرور
        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            bool isOtpValid = await VerifyOtpAsync(new VerifyOtpDto
            {
                PhoneNumber = dto.PhoneNumber,
                OtpCode = dto.OtpCode
            });

            if (!isOtpValid)
                throw new UnauthorizedAccessException("Invalid OTP");

            var user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (user == null)
                throw new NotFoundException("User not found");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await userManager.UpdateAsync(user);
        }

        public async Task CheckPhoneNumberExistsAsync(PhoneNumberDto phoneNumberDto)
        {
            var existingPhoneUser = await userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumberDto.PhoneNumber);

            if (existingPhoneUser is not null)
                throw new V_Exception.ValidationException(new[] { "The phone number is already in use" });
        }
        public async Task CheckEmailExistsAsync(EmailDto emailDto)
        {
            var existingEmailUser = await userManager.Users.FirstOrDefaultAsync(e => e.Email == emailDto.Email);

            if (existingEmailUser is not null)
                throw new V_Exception.ValidationException(new[] { "The Email is already in use" });
        }


        // دالة تسجيل الدخول الخارجي
        public async Task<UserLoginResultDto> ExternalLoginAsync(ExternalAuthDto model)
        {
           
            if (model.Provider.ToLower() == "google")
            {
                var payload = await VerifyExternalToken(model); // تحقق من صحة التوكن (شرحها بعد قليل)

                if (payload == null)
                    throw new UnauthorizedAccessException("Invalid external authentication.");
            }
            else if (model.Provider.ToLower() == "facebook")
            {
                var verifyUrl = $"https://graph.facebook.com/me?access_token={model.IdToken}&fields=id,name,email";
                using var client = new HttpClient();


                var response = await client.GetStringAsync(verifyUrl);

                var userData = JsonConvert.DeserializeObject<FacebookUserDto>(response);


                var user = await userManager.FindByEmailAsync(userData.Email);
                if (user == null)
                {
                    user = new AppUser
                    {
                        Email = userData.Email,
                        UserName = userData.Email,
                        FullName = userData.Name,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(user);
                }

                var token = await GenerateJwtTokenAsync(user);

                return new UserLoginResultDto
                {
                    Email = user.Email,
                    Token = token
                };
            }
            throw new Exception("Unsupported provider");

        }

        // Send OTP using Twilio
        public async Task SendOtpUsingTwilioAsync(PhoneNumberDto phoneNumberDto)
        {
            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            await redisService.SetAsync($"otp:{phoneNumberDto.PhoneNumber}", otpCode, TimeSpan.FromMinutes(5));

            var settings = twilioSettings.Value;
            TwilioClient.Init(settings.AccountSid, settings.AuthToken);

            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber("whatsapp:" + phoneNumberDto.PhoneNumber),
                from: new PhoneNumber(settings.FromNumber),
                body: $"رمز التحقق الخاص بك هو: {otpCode}"
            );
        }

        // دالة تسجيل الخروج
        public async Task LogoutAsync(TokenDto dto)
        {
            var tokenFromCookies = httpContextAccessor.HttpContext.Request.Cookies["JWT_Token"];

            // التحقق من تطابق الـ token في الكوكيز مع الـ token المرسل
            if (string.IsNullOrEmpty(tokenFromCookies) || tokenFromCookies != dto.Token)
            {
                throw new UnauthorizedAccessException("Invalid or mismatched token.");
            }

            // إزالة الجلسة من Redis
            await redisService.RemoveAsync($"session:{dto.Token}");

            // حذف الكوكيز
            var response = httpContextAccessor.HttpContext.Response;
            response.Cookies.Delete("JWT_Token");
            response.Cookies.Delete("Refresh_Token");
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

        // دالة  انشاء ال token
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
                return await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == usernameOrPhone);
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
                UserName = model.FirstName + model.LastName + Guid.NewGuid().ToString("N").Substring(0, 8),
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = new Address
                {

                    City = model.City,        // ديناميكي من DTO
                    Country = "Egypt"         // قيمة ثابتة
                }
            };
        }

        private async Task CreateUserAsync(AppUser user, string password)
        {
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new V_Exception.ValidationException(result.Errors.Select(e => e.Description));
        }

        private UserRegisterationResultDto MapToUserResultDto(AppUser user)
        {
            return new UserRegisterationResultDto
            {
                Email = user.Email
            };
        }

      
    }
}
