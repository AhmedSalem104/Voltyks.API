using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.DTOs.SmsEgyptDTOs;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Application.Interfaces.SMSEgypt
{
    public interface ISmsEgyptService
    {
        Task<ApiResponse<string>> SendOtpAsync(SendOtpDto dto);
        Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto);

        // توليد رمز التحقق (OTP)
        string GenerateOtp();

        // حفظ OTP في الـ Redis
        Task SaveOtpAsync(string phoneNumber, string otp);

        // إرسال رسالة OTP عبر API SMS
        Task<bool> SendOtpMessageAsync(string phoneNumber, string otp);


        // --------------- New methods for Forget Password OTP -----------------

        /// <summary>
        /// Start Forget Password process by sending OTP
        /// </summary>
        Task<ApiResponse<string>> ForgetPasswordAsync(ForgetPasswordDto dto);

        /// <summary>
        /// Verify OTP for Forget Password scenario
        /// </summary>
        Task<ApiResponse<string>> VerifyForgetPasswordOtpAsync(VerifyForgetPasswordOtpDto dto);

        /// <summary>
        /// Reset password after OTP verification
        /// </summary>
        Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto dto);

        /// <summary>
        /// Check if the OTP verification key exists in Redis (for allowing password reset)
        /// </summary>

 
    }

}
