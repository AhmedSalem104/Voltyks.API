using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces;
using Voltyks.Application.ServicesManager;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.DTOs.SmsEgyptDTOs;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services
{
    public class SmsEgyptService : ISmsEgyptService
    {
        private readonly IRedisService _redisService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<SmsEgyptSettings> _smsSettings;
        private readonly UserManager<AppUser> _userManager;
        

        private const int MaxAttempts = 5;
        private readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(2);

        public SmsEgyptService(IRedisService redisService, IHttpClientFactory httpClientFactory, IOptions<SmsEgyptSettings> smsSettings , UserManager<AppUser> userManager)
        {
            _redisService = redisService;
            _httpClientFactory = httpClientFactory;
            _smsSettings = smsSettings;
            _userManager = userManager;
        }



        public async Task<ApiResponse<string>> SendOtpAsync(SendOtpDto dto)
        {
            var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber); // ✅ توحيد تنسيق الرقم

            if (await IsBlockedAsync(normalizedPhone))
            {
                return new ApiResponse<string>(
                    $"لقد تجاوزت الحد الأقصى لمحاولات إرسال OTP. يرجى المحاولة بعد {(int)BlockDuration.TotalSeconds} ثانية.",
                    false);
            }

            int attempts = await GetCurrentAttemptsAsync(normalizedPhone);
            attempts++;

            if (attempts > MaxAttempts)
            {
                await BlockUserAsync(normalizedPhone);
                return new ApiResponse<string>(
                    $"لقد تجاوزت الحد الأقصى لمحاولات إرسال OTP. تم حظرك لمدة {(int)BlockDuration.TotalMinutes} دقائق.",
                    false);
            }

            await SaveAttemptsAsync(normalizedPhone, attempts);

            var otp = GenerateOtp();
            await SaveOtpAsync(normalizedPhone, otp);

            var isSent = await SendOtpMessageAsync(normalizedPhone, otp);
            if (!isSent)
                return new ApiResponse<string>("Failed to send OTP", false);

            return new ApiResponse<string>("OTP sent successfully", true);
        }

        public async Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto)
        {

            var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber);

            var cachedOtp = await _redisService.GetAsync($"otp:{normalizedPhone}");

            if (string.IsNullOrEmpty(cachedOtp))
            {
                return new ApiResponse<string>("OTP expired or not found", false);
            }

            if (cachedOtp != dto.OtpCode)
            {
                return new ApiResponse<string>("Invalid OTP", false);
            }

            await _redisService.RemoveAsync($"otp:{normalizedPhone}");

            return new ApiResponse<string>("OTP verified successfully.");
        }




        public async Task<ApiResponse<string>> ForgetPasswordAsync(ForgetPasswordDto dto)
        {
            // توحيد رقم الهاتف
            var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber);

            // التأكد من وجود المستخدم بهذا الرقم
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (user == null)
            {
                return new ApiResponse<string>("رقم الهاتف غير موجود", false);
            }

            // تحقق من الحظر بسبب المحاولات السابقة (بنفس فكرة SmsEgyptService)
            if (await _redisService.GetAsync($"otp_block:{normalizedPhone}") != null)
            {
                return new ApiResponse<string>($"لقد تجاوزت الحد الأقصى لمحاولات إرسال OTP. يرجى المحاولة لاحقًا.", false);
            }

            // توليد OTP (يمكن تستخدم دالة GenerateOtp() الموجودة في SmsEgyptService)
            var otp = GenerateOtp();

            // حفظ OTP في Redis لمدة 5 دقائق (مثلاً)
            await _redisService.SetAsync($"forget_password_otp:{normalizedPhone}", otp, TimeSpan.FromMinutes(5));

            // إرسال رسالة OTP للمستخدم (يمكن إعادة استخدام SendOtpMessageAsync من SmsEgyptService)
            var isSent = await SendOtpMessageAsync(normalizedPhone, otp);

            if (!isSent)
                return new ApiResponse<string>("فشل في إرسال رمز التحقق OTP", false);

            return new ApiResponse<string>("تم إرسال رمز التحقق OTP بنجاح", true);
        }
        public async Task<ApiResponse<string>> VerifyForgetPasswordOtpAsync(VerifyForgetPasswordOtpDto dto)
        {
            var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber);
            var cachedOtp = await _redisService.GetAsync($"forget_password_otp:{normalizedPhone}");

            if (string.IsNullOrEmpty(cachedOtp))
            {
                return new ApiResponse<string>("رمز التحقق OTP انتهت صلاحيته أو غير موجود", false);
            }

            if (cachedOtp != dto.OtpCode)
            {
                return new ApiResponse<string>("رمز التحقق OTP غير صحيح", false);
            }

            // إزالة OTP من Redis بعد التحقق الناجح
            await _redisService.RemoveAsync($"forget_password_otp:{normalizedPhone}");

            // يمكن حفظ علامة لتأكيد التحقق لإعادة تعيين كلمة المرور (مثلاً)
            await _redisService.SetAsync($"forget_password_verified:{normalizedPhone}", "verified", TimeSpan.FromMinutes(10));

            return new ApiResponse<string>("تم التحقق من رمز OTP بنجاح", true);
        }
        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var normalizedPhone = NormalizePhoneNumber(resetPasswordDto.PhoneNumber);

            // التحقق من وجود التحقق الناجح (OTP verified)
            var verified = await _redisService.GetAsync($"forget_password_verified:{normalizedPhone}");
            if (verified != "verified")
            {
                return new ApiResponse<string>("لم يتم التحقق من رمز OTP أو انتهت صلاحيته", false);
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (user == null)
            {
                return new ApiResponse<string>("المستخدم غير موجود", false);
            }

            // إزالة كلمة المرور الحالية (إذا كان المستخدم مسجل بكلمة مرور)
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                return new ApiResponse<string>("حدث خطأ أثناء إزالة كلمة المرور القديمة", false);
            }

            // إضافة كلمة مرور جديدة
            var addResult = await _userManager.AddPasswordAsync(user, resetPasswordDto.NewPassword);
            if (!addResult.Succeeded)
            {
                return new ApiResponse<string>("حدث خطأ أثناء تعيين كلمة المرور الجديدة", false);
            }

            // إزالة علامة التحقق بعد النجاح
            await _redisService.RemoveAsync($"forget_password_verified:{normalizedPhone}");

            return new ApiResponse<string>("تم إعادة تعيين كلمة المرور بنجاح", true);
        }





        // ---------- Private Methods ----------
        private string NormalizePhoneNumber(string phone)
        {
            if (phone.StartsWith("+20"))
                phone = phone.Replace("+20", "0");
            else if (phone.StartsWith("0020"))
                phone = phone.Replace("0020", "0");

            return phone;
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

        public string GenerateOtp()
        {
            return new Random().Next(1000, 9999).ToString();
        }

        public async Task SaveOtpAsync(string phoneNumber, string otp)
        {
            await _redisService.SetAsync($"otp:{phoneNumber}", otp, TimeSpan.FromMinutes(5));
        }

        public async Task<bool> SendOtpMessageAsync(string phoneNumber, string otp)
        {
            string message = $"Your OTP is {otp}";
            string fullUrl = $"{_smsSettings.Value.BaseUrl}?username={_smsSettings.Value.Username}&password={_smsSettings.Value.Password}&sendername={_smsSettings.Value.SenderName}&message={Uri.EscapeDataString(message)}&mobiles={phoneNumber}";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(fullUrl);

            return response.IsSuccessStatusCode;
        }

        
    }

}
