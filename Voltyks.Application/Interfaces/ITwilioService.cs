using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.TwilioConfDTOs;

namespace Voltyks.Application.Interfaces
{
    public interface ITwilioService
    {
        Task SendWhatsAppMessageAsync(WhatsAppMessageDto dto);
        Task SendOtpAsync(SendOtpDto dto);
        Task<bool> VerifyOtpAsync(string phoneNumber, string inputOtp);

    }

}
