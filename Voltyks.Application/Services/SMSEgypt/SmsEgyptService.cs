using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces.Redis;
using Voltyks.Application.Interfaces.SMSEgypt;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.DTOs.SmsEgyptDTOs;
using Voltyks.Core.Exceptions;
using Voltyks.Persistence.Entities;
using Voltyks.Persistence.Entities.Identity;
using Microsoft.Extensions.Logging;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services.SMSEgypt
{
    public class SmsEgyptService : ISmsEgyptService
    {
        private readonly IRedisService _redisService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<SmsEgyptSettings> _smsSettings;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<SmsEgyptService> _logger;
        private const int MaxAttempts = 10;
        private readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(10);

        public SmsEgyptService(IRedisService redisService, IHttpClientFactory httpClientFactory, IOptions<SmsEgyptSettings> smsSettings, UserManager<AppUser> userManager, ILogger<SmsEgyptService> logger)
        {
            _redisService = redisService;
            _httpClientFactory = httpClientFactory;
            _smsSettings = smsSettings;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ApiResponse<string>> SendOtpAsync(SendOtpDto dto)
        {
            var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber);

            /* 1️⃣ تحقق من حد الرسائل اليومية */
            var dailyLimitResult = await CheckAndIncrementOtpDailyLimitAsync(normalizedPhone);
            if (!dailyLimitResult.Status)
                return new ApiResponse<string>(dailyLimitResult.Message, false);

            /* 2️⃣ تحقق من الحظر المؤقت بسبب إدخال OTP خطأ أكثر من المسموح */
            if (await IsBlockedAsync(normalizedPhone))
            {
                return new ApiResponse<string>(
                    string.Format(ErrorMessages.OtpAttemptLimitExceededTryLater, (int)BlockDuration.TotalSeconds),
                    false);
            }

            /* 3️⃣ عدّ المحاولات الفاشلة لإدخال الـ OTP (حماية من التحايل) */
            int attempts = await GetCurrentAttemptsAsync(normalizedPhone);
            attempts++;
            if (attempts > MaxAttempts)
            {
                await BlockUserAsync(normalizedPhone);
                return new ApiResponse<string>(
                    string.Format(ErrorMessages.OtpAttemptsExceededBlockedForMinutes, (int)BlockDuration.TotalMinutes),
                    false);
            }
            await SaveAttemptsAsync(normalizedPhone, attempts);

            /* 4️⃣ توليد وحفظ الـ OTP ثم إرسالها */
            var otp = GenerateOtp();
            await SaveOtpAsync(normalizedPhone, otp);

            var isSent = await SendOtpMessageAsync(normalizedPhone, otp);
            if (!isSent)
                return new ApiResponse<string>(ErrorMessages.OTPSendingFailed, false);

            return new ApiResponse<string>(SuccessfulMessage.OtpSentSuccessfully, true);
        }
        public async Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber);
            var otpKey = $"otp:{normalizedPhone}";
            var attemptsKey = $"otp_attempts:{normalizedPhone}";
            var blockKey = $"otp_block:{normalizedPhone}";

            var cachedOtp = await _redisService.GetAsync(otpKey);
            if (string.IsNullOrEmpty(cachedOtp))
                return new ApiResponse<string>(ErrorMessages.OtpCodeInvalid, false);

            if (cachedOtp != dto.OtpCode)
                return new ApiResponse<string>(ErrorMessages.OtpCodeInvalid, false);

            // ✅ Success – wipe transient state
            await _redisService.RemoveAsync(otpKey);
            await _redisService.RemoveAsync(attemptsKey);   // NEW
            await _redisService.RemoveAsync(blockKey);      // NEW

            return new ApiResponse<string>(SuccessfulMessage.OtpVerifiedSuccessfully, true);
        }
        public async Task<ApiResponse<string>> ForgetPasswordAsync(ForgetPasswordDto dto)
        {
            // 1️⃣ جيب المستخدم سواء أدخل رقم أو إيميل
            var user = await GetUserByUsernameOrPhoneAsync(dto.EmailOrPhone);
            if (user == null)
                return new ApiResponse<string>(ErrorMessages.PhoneNumberNotExist, false);

            // 2️⃣ استخرج رقم الموبايل من المستخدم
            var normalizedPhone = NormalizePhoneNumber(user.PhoneNumber);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
                return new ApiResponse<string>("User phone number is missing or invalid", false);

            // 3️⃣ تحقق من حد الرسائل اليومية
            var dailyLimitResult = await CheckAndIncrementOtpDailyLimitAsync(normalizedPhone);
            if (!dailyLimitResult.Status)
                return new ApiResponse<string>(dailyLimitResult.Message, false);

            // 4️⃣ تحقق من الحظر المؤقت
            if (await _redisService.GetAsync($"otp_block:{normalizedPhone}") != null)
                return new ApiResponse<string>(ErrorMessages.ExceededMaximumOTPAttempts, false);

            // 5️⃣ توليد وإرسال OTP
            var otp = GenerateOtp();
            await _redisService.SetAsync($"forget_password_otp:{normalizedPhone}", otp, TimeSpan.FromMinutes(5));

            var isSent = await SendOtpMessageAsync(normalizedPhone, otp);
            if (!isSent)
                return new ApiResponse<string>(ErrorMessages.OTPSendingFailed, false);

            return new ApiResponse<string>(SuccessfulMessage.OtpSentSuccessfully, true);
        }
        public async Task<ApiResponse<string>> VerifyForgetPasswordOtpAsync(VerifyForgetPasswordOtpDto dto)
        {
            var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber);
            var cachedOtp = await _redisService.GetAsync($"forget_password_otp:{normalizedPhone}");

            if (string.IsNullOrEmpty(cachedOtp))
            {
                return new ApiResponse<string>(ErrorMessages.OtpCodeExpiredOrNotFound, false);
            }

            if (cachedOtp != dto.OtpCode)
            {
                return new ApiResponse<string>(ErrorMessages.OtpCodeInvalid, false);
            }

            // إزالة OTP من Redis بعد التحقق الناجح
            await _redisService.RemoveAsync($"forget_password_otp:{normalizedPhone}");

            // يمكن حفظ علامة لتأكيد التحقق لإعادة تعيين كلمة المرور (مثلاً)
            await _redisService.SetAsync($"forget_password_verified:{normalizedPhone}", "verified", TimeSpan.FromMinutes(10));

            return new ApiResponse<string>(SuccessfulMessage.OtpVerifiedSuccessfully, true);
        }
        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var normalizedPhone = NormalizePhoneNumber(resetPasswordDto.PhoneNumber);

            // التحقق من وجود التحقق الناجح (OTP verified)
            var verified = await _redisService.GetAsync($"forget_password_verified:{normalizedPhone}");
            if (verified != "verified")
            {
                return new ApiResponse<string>(ErrorMessages.OtpCodeNotVerifiedOrExpired, false);
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (user == null)
            {
                return new ApiResponse<string>(ErrorMessages.UserNotFound, false);
            }

            // إزالة كلمة المرور الحالية (إذا كان المستخدم مسجل بكلمة مرور)
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                return new ApiResponse<string>(ErrorMessages.ErrorRemovingOldPassword, false);
            }

            // إضافة كلمة مرور جديدة
            var addResult = await _userManager.AddPasswordAsync(user, resetPasswordDto.NewPassword);
           
           if (!addResult.Succeeded)
            {
                var identityErrors = addResult.Errors.Select(e => e.Description).ToList();

                return new ApiResponse<string>(
                    message: ErrorMessages.ErrorSettingNewPassword,  
                    status: false,
                    errors: identityErrors                 
                );
            }


            // إزالة علامة التحقق بعد النجاح
            await _redisService.RemoveAsync($"forget_password_verified:{normalizedPhone}");

            return new ApiResponse<string>(SuccessfulMessage.PasswordResetSuccessfully, true);
        }
        public string GenerateOtp()
        {
            return new Random().Next(1000, 9999).ToString();
        }
        public async Task SaveOtpAsync(string phoneNumber, string otp)
        {
            await _redisService.SetAsync($"otp:{phoneNumber}", otp, TimeSpan.FromMinutes(5));
        }
        public async Task<bool> SendOtpMessageAsync(string phoneNumber, string otp, string? customMessage = null)
        {
            string message = customMessage ?? $"Your Voltyks verification code is {otp}";
            string fullUrl = $"{_smsSettings.Value.BaseUrl}?username={_smsSettings.Value.Username}&password={_smsSettings.Value.Password}&sendername={_smsSettings.Value.SenderName}&message={Uri.EscapeDataString(message)}&mobiles={phoneNumber}";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(fullUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SMS Egypt API returned HTTP {StatusCode} for phone {Phone}. Body: {Body}",
                    (int)response.StatusCode, phoneNumber, responseBody);
                return false;
            }

            _logger.LogInformation("SMS Egypt response for phone {Phone}: {Body}", phoneNumber, responseBody);
            return true;
        }

        // ---------- Private Methods ----------
        private async Task<ApiResponse<bool>> CheckAndIncrementOtpDailyLimitAsync(string phoneNumber)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            var key = $"otp_daily_limit:{normalizedPhone}";

            long newCount = await _redisService.IncrementAsync(key);

            // نخلي المفتاح ينتهي بعد 24 ساعة من أول محاولة
            if (newCount == 1)
                await _redisService.ExpireAsync(key, TimeSpan.FromHours(24));

            //  نخليه 20
            if (newCount > 20)
            {
                // ممكن كمان نحط Log أو Alert لو محتاج تتابع حالات التعدي
                return new ApiResponse<bool>(ErrorMessages.OtpLimitExceededForToday, false);
            }

            return new ApiResponse<bool>(true);
        }
        private async Task<AppUser?> GetUserByUsernameOrPhoneAsync(string usernameOrPhone)
        {
            if (usernameOrPhone.Contains("@"))
            {
                var emailUser = await _userManager.FindByEmailAsync(usernameOrPhone);
                if (emailUser == null)
                    throw new UnAuthorizedException("Email does not exist");

                if (string.IsNullOrEmpty(emailUser.PhoneNumber))
                    throw new UnAuthorizedException("There is no phone number associated with this mail");

                return await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == emailUser.PhoneNumber);
            }
            else
            {
                // توحيد رقم الهاتف
                var normalizedPhone = NormalizePhoneNumber(usernameOrPhone);
                return await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            }
        }
        private string NormalizePhoneNumber(string input)
        {
            // لو الشكل المحلي زي 010xxxxxxxx
            if (Regex.IsMatch(input, @"^01[0-9]{9}$"))
            {
                return $"+2{input}";
            }

            // لو الشكل الدولي زي +2010xxxxxxxx
            if (Regex.IsMatch(input, @"^\+201[0-9]{9}$"))
            {
                return input;
            }

            // لو إيميل، نرجعه زي ما هو
            if (new EmailAddressAttribute().IsValid(input))
            {
                return input;
            }

            throw new ArgumentException("Invalid input. Accepted formats: 010xxxxxxxx, +2010xxxxxxxx, or valid email address.");
        }
        private async Task<bool> IsBlockedAsync(string phoneNumber)
        {
            string blockKey = $"otp_block:{phoneNumber}";
            var isBlocked = await _redisService.GetAsync(blockKey);
            return isBlocked != null;
        }
        private async Task<int> GetCurrentAttemptsAsync(string phoneNumber)
        {
            string attemptsKey = $"otp_attempts:{phoneNumber}";
            var attemptsStr = await _redisService.GetAsync(attemptsKey);

            return !string.IsNullOrEmpty(attemptsStr) && int.TryParse(attemptsStr, out int parsedAttempts)
                ? parsedAttempts
                : 0;
        }
        private async Task BlockUserAsync(string phoneNumber)
        {
            string blockKey = $"otp_block:{phoneNumber}";
            string attemptsKey = $"otp_attempts:{phoneNumber}";

            await _redisService.SetAsync(blockKey, "blocked", BlockDuration);
            await _redisService.RemoveAsync(attemptsKey);
        }
        private async Task SaveAttemptsAsync(string phoneNumber, int attempts)
        {
            string attemptsKey = $"otp_attempts:{phoneNumber}";
            await _redisService.SetAsync(attemptsKey, attempts.ToString(), BlockDuration);
        }
        public async Task<ApiResponse<string>> ClearOtpDailyLimitAsync(string phoneNumber)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            await _redisService.RemoveAsync($"otp_daily_limit:{normalizedPhone}");
            return new ApiResponse<string>("Daily OTP limit cleared successfully", true);
        }

    }

}
