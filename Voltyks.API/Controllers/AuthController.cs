using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.Exceptions;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Voltyks.Core.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace Voltyks.Presentation
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IServiceManager serviceManager;

        public AuthController(IServiceManager serviceManager)
        {
            this.serviceManager = serviceManager;
        }

        [HttpGet]
        public IActionResult GetInfo()
        {
            var info = new
            {
                OS = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString()
            };
            return Ok(info);
        }


        // تسجيل الدخول    
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            try
            {
                var result = await serviceManager.AuthService.LoginAsync(loginDTO);
                return Ok(new ApiResponse<object>(result, "Login successful."));
            }
            catch (UnAuthorizedException ex)
            {
                return Unauthorized(new ApiResponse<string>(ex.Message, false));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(ex.Message, false));
            }
        }

        // تسجيل مستخدم جديد
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            try
            {
                var result = await serviceManager.AuthService.RegisterAsync(registerDTO);

                var response = new ApiResponse<UserRegisterationResultDto>(
                    data: result,
                    message: $"Welcome, {result.Email}! Your email has been successfully registered."
                );

                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var response = new ApiResponse<string>(
                    message: "Validation failed",
                    status: false
                )
                {
                    Data = string.Join("; ", ex.Errors)
                };

                return BadRequest(response);
            }
            catch (Exception ex)
            {
                var response = new ApiResponse<string>(
                    message: ex.Message,
                    status: false
                );

                return StatusCode(500, response);
            }
        }

        // تحديث JWT باستخدام refresh token
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshJwtToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var token = await serviceManager.AuthService.RefreshJwtTokenAsync(refreshTokenDto);
                return Ok(new ApiResponse<string>(token, "Token refreshed successfully"));
            }
            catch (SecurityTokenExpiredException ex)
            {
                return Unauthorized(new ApiResponse<string>(ex.Message, false));
            }
            catch (UnAuthorizedException ex)
            {
                return Unauthorized(new ApiResponse<string>(ex.Message, false));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(ex.Message, false));
            }
        }

        // إرسال OTP
        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] PhoneNumberDto phoneNumberDto)
        {
            try
            {
                await serviceManager.AuthService.SendOtpAsync(phoneNumberDto);
                return Ok(new ApiResponse<string>("OTP sent successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(ex.Message, false));
            }
        }

        // التحقق من OTP
        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromQuery] VerifyOtpDto verifyOtpDto)
        {
            try
            {
                bool isValid = await serviceManager.AuthService.VerifyOtpAsync(verifyOtpDto);
                return isValid
                    ? Ok(new ApiResponse<string>("OTP verified successfully."))
                    : Unauthorized(new ApiResponse<string>("Invalid OTP.", false));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(ex.Message, false));
            }
        }

        // إرسال OTP لاستعادة كلمة المرور
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] PhoneNumberDto phoneNumberDto)
        {
            try
            {
                await serviceManager.AuthService.ForgotPasswordAsync(phoneNumberDto);
                return Ok(new ApiResponse<string>("OTP sent to reset your password."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(ex.Message, false));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(ex.Message, false));
            }
        }

        // إعادة تعيين كلمة المرور
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromQuery] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                await serviceManager.AuthService.ResetPasswordAsync(resetPasswordDto);
                return Ok(new ApiResponse<string>("Password reset successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<string>(ex.Message, false));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(ex.Message, false));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(ex.Message, false));
            }
        }

        // تسجيل الدخول بواسطة مزود خارجي
        [HttpPost("external-login")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalAuthDto dto)
        {
            try
            {
                var result = await serviceManager.AuthService.ExternalLoginAsync(dto);
                return Ok(new ApiResponse<UserLoginResultDto>(result, "External login successful."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(ex.Message, false));
            }
        }

        // إرسال OTP عبر Twilio
        [HttpPost("send-otp/twilio")]
        public async Task<IActionResult> SendOtpViaTwilio([FromBody] PhoneNumberDto phoneNumberDto)
        {
            try
            {
                await serviceManager.AuthService.SendOtpUsingTwilioAsync(phoneNumberDto);
                return Ok(new ApiResponse<string>("OTP sent via Twilio."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(ex.Message, false));
            }
        }
        // التحقق من توفر  رقم التليفون

        [HttpPost("CheckPhoneNumberExists")]
        public async Task<IActionResult> CheckPhoneNumberExists([FromBody] PhoneNumberDto phoneNumberDto)
        {
            if (string.IsNullOrWhiteSpace(phoneNumberDto?.PhoneNumber))
                return BadRequest(new ApiResponse<string>("Phone number is required", false));

            try
            {
                await serviceManager.AuthService.CheckPhoneNumberExistsAsync(phoneNumberDto);
                return Ok(new ApiResponse<string>("✅ The phone number is available."));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object>("❌ The phone number is already in use", false)
                {
                    
                });
            }
        }

        // التحقق من توفر البريد الإلكتروني

        [HttpPost("CheckEmailExists")]
        public async Task<IActionResult> CheckEmailExists([FromBody] EmailDto emailDto)
        {
            if (string.IsNullOrWhiteSpace(emailDto?.Email))
                return BadRequest(new ApiResponse<string>("Email is required", false));

            try
            {
                await serviceManager.AuthService.CheckEmailExistsAsync(emailDto);
                return Ok(new ApiResponse<string>("✅ The email is available."));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object>("❌ The email is already in use", false)
                {
                    
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
