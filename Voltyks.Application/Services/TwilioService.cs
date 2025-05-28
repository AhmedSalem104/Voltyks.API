using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.Types;
using Twilio;
using Voltyks.Application.Interfaces;
using StackExchange.Redis;
using Voltyks.Core.DTOs.TwilioConfDTOs;
using Voltyks.Persistence.Entities.Main;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;

namespace Voltyks.Application.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly TwilioSettings _settings;
        private readonly IRedisService _redisService;

        public TwilioService(IOptions<TwilioSettings> options, IRedisService redisService)
        {
            _settings = options.Value;
            _redisService = redisService;
            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
        }

        public async Task SendWhatsAppMessageAsync(WhatsAppMessageDto dto)
        {
            try
            {
                // Step 1: Init the Twilio client
                TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);

                // Step 2: Prepare the message
                var messageOptions = new CreateMessageOptions(
                    new PhoneNumber($"whatsapp:{dto.To}"))
                {
                    From = new PhoneNumber($"whatsapp:{_settings.FromNumber}"),
                    Body = dto.Message
                };

                // Step 3: Send it
                var message = await MessageResource.CreateAsync(messageOptions);

                // Optional: Log the message SID
                Console.WriteLine($"Message SID: {message.Sid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Twilio Error: {ex.Message}");
                throw;
            }
        }


        //public async Task SendWhatsAppMessageAsync(WhatsAppMessageDto dto)
        //{
        //    var messageOptions = new CreateMessageOptions(
        //        new PhoneNumber($"whatsapp:{dto.To}"))
        //    {
        //        From = new PhoneNumber($"whatsapp:{_settings.FromNumber}"),
        //        Body = dto.Message
        //    };

        //    await MessageResource.CreateAsync(messageOptions);
        //}

        public async Task SendOtpAsync(SendOtpDto dto)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            var message = $"رمز التحقق الخاص بك هو: {otp}";

            await SendWhatsAppMessageAsync(new WhatsAppMessageDto
            {
                To = dto.PhoneNumber,
                Message = message
            });

            var key = $"otp:{dto.PhoneNumber}";
            await _redisService.SetAsync(key, otp, TimeSpan.FromMinutes(5));
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string inputOtp)
        {
            var key = $"otp:{phoneNumber}";
            var savedOtp = await _redisService.GetAsync(key);

            return savedOtp != null && savedOtp == inputOtp;
        }
    }

}
