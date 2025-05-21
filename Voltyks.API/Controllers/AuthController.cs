using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.Exceptions;
using System;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.TwilioConfDTOs;
using Voltyks.Application.Interfaces;
using VerifyOtpDto = Voltyks.Core.DTOs.TwilioConfDTOs.VerifyOtpDto;

namespace Voltyks.Presentation
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IServiceManager serviceManager;
        private readonly ITwilioService _twilioService;


        public AuthController(IServiceManager serviceManager , ITwilioService twilioService)
        {
            this.serviceManager = serviceManager;
            this._twilioService = twilioService;

        }

        // تسجيل الدخول
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            try
            {
                var result = await serviceManager.AuthService.LoginAsync(loginDTO);
                return Ok(result);
            }
            catch (UnAuthorizedException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // تسجيل مستخدم جديد
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            try
            {
                var result = await serviceManager.AuthService.RegisterAsync(registerDTO);
                return Ok($"Welcome, {result.FullName}! Your email has been successfully registered");
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // تحديث JWT باستخدام refresh token
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshJwtToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await serviceManager.AuthService.RefreshJwtTokenAsync(refreshTokenDto);
                return Ok(result);
            }
            catch (UnAuthorizedException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        {
            await _twilioService.SendOtpAsync(dto);
            return Ok(new { message = "OTP sent successfully" });
        }

        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var result = await _twilioService.VerifyOtpAsync(dto.PhoneNumber, dto.Code);
            if (!result)
                return BadRequest("Invalid or expired OTP.");

            return Ok(new { message = "OTP verified successfully" });
        }



        //// إرسال OTP
        //[HttpPost("SendOtp")]
        //public async Task<IActionResult> SendOtp([FromBody] PhoneNumberDto phoneNumberDto)
        //{
        //    try
        //    {
        //        await serviceManager.AuthService.SendOtpAsync(phoneNumberDto);
        //        return Ok("OTP sent successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        //// التحقق من OTP
        //[HttpPost("VerifyOtp")]
        //public async Task<IActionResult> VerifyOtp([FromQuery] VerifyOtpDto verifyOtpDto)
        //{
        //    try
        //    {
        //        bool isValid = await serviceManager.AuthService.VerifyOtpAsync(verifyOtpDto);
        //        return isValid ? Ok("OTP verified successfully.") : Unauthorized("Invalid OTP.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        // إرسال OTP لاستعادة كلمة المرور
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] PhoneNumberDto phoneNumberDto)
        {
            try
            {
                await serviceManager.AuthService.ForgotPasswordAsync(phoneNumberDto);
                return Ok("OTP sent to reset your password.");
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // إعادة تعيين كلمة المرور
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromQuery] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                await serviceManager.AuthService.ResetPasswordAsync(resetPasswordDto);
                return Ok("Password reset successfully.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // تسجيل الدخول بواسطة مزود خارجي
        [HttpPost("external-login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserResultDto>> ExternalLogin([FromBody] ExternalAuthDto dto)
        {
            return await serviceManager.AuthService.ExternalLoginAsync(dto);
        }

        // إرسال OTP عبر Twilio
        [HttpPost("send-otp/twilio")]
        public async Task<IActionResult> SendOtpViaTwilio([FromBody] PhoneNumberDto phoneNumberDto)
        {
            await serviceManager.AuthService.SendOtpUsingTwilioAsync(phoneNumberDto);
            return Ok("OTP sent via Twilio");
        }

        // التحقق من توفر البريد الإلكتروني
        [HttpPost("CheckEmailExists")]
        public async Task<IActionResult> CheckEmailExists([FromBody] EmailDto emailDto)
        {
            if (string.IsNullOrWhiteSpace(emailDto?.Email))
            {
                return BadRequest("Email is required.");
            }

            try
            {
                await serviceManager.AuthService.CheckEmailExistsAsync(emailDto);
                return Ok("✅ The email is available.");
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    Message = "❌ Email is already use.",
                    Details = ex.Errors
                });
            }
        }

        // التحقق من توفر رقم الهاتف
        [HttpPost("CheckPhoneNumberExists")]
        public async Task<IActionResult> CheckPhoneNumberExists([FromBody] PhoneNumberDto phoneNumberDto)
        {
            if (string.IsNullOrWhiteSpace(phoneNumberDto?.PhoneNumber))
            {
                return BadRequest("Phone number is required.");
            }

            try
            {
                await serviceManager.AuthService.CheckPhoneNumberExistsAsync(phoneNumberDto);
                return Ok("✅ The phone number is available.");
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    Message = "❌ Phone number is already use.",
                    Details = ex.Errors
                });
            }
        }

        // تسجيل الخروج
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] TokenDto tokenDto)
        {
            try
            {
                // التحقق من أن الـ token المرسل يتطابق مع المخزن
                await serviceManager.AuthService.LogoutAsync(tokenDto);
                return Ok("Logged out successfully.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);  // إرجاع حالة 401 إذا كان الـ token غير صالح أو غير متطابق
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);  // إرجاع حالة 400 في حالة حدوث خطأ آخر
            }
        }


    }
}
