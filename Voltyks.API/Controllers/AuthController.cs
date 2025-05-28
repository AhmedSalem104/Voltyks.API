using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.Exceptions;
using Voltyks.Application.Interfaces;
using SendSmsEgyptOtpDto = Voltyks.Core.DTOs.SmsEgyptDTOs.SendOtpDto;
using SendSmsBeOnOtpDto = Voltyks.Core.DTOs.SmsBeOnDTOs.SendOtpDto;

using SmsEgyptVerifyOtpDto = Voltyks.Core.DTOs.SmsEgyptDTOs.VerifyOtpDto;
using SmsBeOnVerifyOtpDto = Voltyks.Core.DTOs.SmsBeOnDTOs.VerifyOtpDto;

using System.Runtime.InteropServices;
using Voltyks.Core.DTOs;
using Microsoft.IdentityModel.Tokens;
using Voltyks.Application;
using Voltyks.Core.DTOs.TwilioConfDTOs;




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

        //[HttpGet]
        //public IActionResult GetInfo()
        //{
        //    var info = new
        //    {
        //        OS = RuntimeInformation.OSDescription,
        //        Architecture = RuntimeInformation.OSArchitecture.ToString()
        //    };
        //    return Ok(info);
        //}

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





        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await serviceManager.SmsEgyptService.ForgetPasswordAsync(model);

            if (!response.Status)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("verify-forget-password-otp")]
        public async Task<IActionResult> VerifyForgetPasswordOtp([FromBody] VerifyForgetPasswordOtpDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await serviceManager.SmsEgyptService.VerifyForgetPasswordOtpAsync(model);

            if (!response.Status)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ممكن تتحقق من OTP هنا لو حابب double check
            var response = await serviceManager.SmsEgyptService.ResetPasswordAsync(model);

            if (!response.Status)
                return BadRequest(response);

            return Ok(response);
        }




        #region SmsEgypt

        [HttpPost("SendSmsEgyptOtp")]
        public async Task<IActionResult> SendOtp([FromBody] SendSmsEgyptOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await serviceManager.SmsEgyptService.SendOtpAsync(dto);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPost("VerifySmsEgyptOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] SmsEgyptVerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await serviceManager.SmsEgyptService.VerifyOtpAsync(dto);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        #endregion

        #region SmsBeOn

        //// Endpoint لإرسال OTP
        //[HttpPost("SendBeOnOtp")]
        //public async Task<IActionResult> SendOtp([FromBody] SendSmsBeOnOtpDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var result = await serviceManager.SmsBeOnService.SendOtpAsync(dto);
        //    return result.Status ? Ok(result) : BadRequest(result);
        //}

        //// Endpoint للتحقق من OTP
        //[HttpPost("VerifyBeOnOtp")]
        //public async Task<IActionResult> VerifyOtp([FromBody] SmsBeOnVerifyOtpDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var result = await serviceManager.SmsBeOnService.VerifyOtpAsync(dto);
        //    return result.Status ? Ok(result) : BadRequest(result);
        //}

        #endregion

        #region Twilio


        //[HttpPost("SendOtp")]
        //public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        //{
        //    await _twilioService.SendOtpAsync(dto);
        //    return Ok(new ApiResponse<string>("OTP sent successfully", true));
        //}

        //[HttpPost("VerifyOtp")]
        //public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        //{
        //    var result = await _twilioService.VerifyOtpAsync(dto.PhoneNumber, dto.Code);
        //    if (!result)
        //        return BadRequest(new ApiResponse<string>("Invalid or expired OTP.", true));

        //    return Ok(new ApiResponse<string>("OTP verified successfully.", true));

        //}


        #endregion
    }
}
