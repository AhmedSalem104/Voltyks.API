using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Voltyks.Application.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.SmsBeOnDTOs;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Services
{
    public class SmsBeOnService : ISmsBeOnService
    {
        private readonly IRedisService _redisService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<SmsBeOnSettings> _smsSettings;

        private const int MaxAttempts = 5;
        private readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(2);

        public SmsBeOnService(IRedisService redisService, IHttpClientFactory httpClientFactory, IOptions<SmsBeOnSettings> smsSettings)
        {
            _redisService = redisService;
            _httpClientFactory = httpClientFactory;
            _smsSettings = smsSettings;
        }

        //public async Task<ApiResponse<string>> SendOtpAsync(SendOtpDto dto)
        //{
        //    var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber); // 👈 Normalize the phone number

        //    if (await IsBlockedAsync(normalizedPhone))
        //    {
        //        return new ApiResponse<string>(
        //            $"You have exceeded the maximum number of OTP attempts. Please try again after {(int)BlockDuration.TotalSeconds} seconds.",
        //            false);
        //    }

        //    int attempts = await GetCurrentAttemptsAsync(normalizedPhone);
        //    attempts++;

        //    if (attempts > MaxAttempts)
        //    {
        //        await BlockUserAsync(normalizedPhone);
        //        return new ApiResponse<string>(
        //            $"You have been blocked for {(int)BlockDuration.TotalMinutes} minutes due to exceeding OTP attempts.",
        //            false);
        //    }

        //    await SaveAttemptsAsync(normalizedPhone, attempts);

        //    var otp = GenerateOtp();
        //    await SaveOtpAsync(normalizedPhone, otp);

        //    var isSent = await SendOtpMessageAsync(normalizedPhone, otp);
        //    if (!isSent)
        //        return new ApiResponse<string>("Failed to send OTP", false);

        //    return new ApiResponse<string>("OTP sent successfully", true);
        //}


        //public async Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto)
        //{

        //    var normalizedPhone = NormalizePhoneNumber(dto.PhoneNumber);

        //    var cachedOtp = await _redisService.GetAsync($"otp:{normalizedPhone}");

        //    if (string.IsNullOrEmpty(cachedOtp))
        //    {
        //        return new ApiResponse<string>("OTP expired or not found", false);
        //    }

        //    if (cachedOtp != dto.OtpCode)
        //    {
        //        return new ApiResponse<string>("Invalid OTP", false);
        //    }

        //    await _redisService.RemoveAsync($"otp:{normalizedPhone}");

        //    return new ApiResponse<string>("OTP verified successfully.");
        //}

        // ----------- Private Methods -----------


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

        private static string GenerateOtp()
        {
            return new Random().Next(1000, 9999).ToString();
        }

        private async Task SaveOtpAsync(string phoneNumber, string otp)
        {
            await _redisService.SetAsync($"otp:{phoneNumber}", otp, TimeSpan.FromMinutes(5));
        }

        private async Task<bool> SendOtpMessageAsync(string phoneNumber, string otp)
        {
            string message = $"Your OTP is {otp}";

            var smsDto = new SmsBeOnMessageDto
            {
                Sender = _smsSettings.Value.SenderName,
                Mobile = phoneNumber,
                Message = message,
                Name = _smsSettings.Value.Name,
                Lang = _smsSettings.Value.Lang,
                Otp_length = _smsSettings.Value.Otp_length,
                Type = _smsSettings.Value.Type
                
            };

            var client = _httpClientFactory.CreateClient();

            // اضافة توكن التفويض في الهيدر
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _smsSettings.Value.Token);

            var json = JsonConvert.SerializeObject(smsDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_smsSettings.Value.BaseUrl, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response: " + responseContent);

            return response.IsSuccessStatusCode;
        }
    }



}
