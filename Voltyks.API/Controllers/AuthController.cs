using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.Exceptions;
using Voltyks.Application.Interfaces;
using SendSmsEgyptOtpDto = Voltyks.Core.DTOs.SmsEgyptDTOs.SendOtpDto;
using SmsEgyptVerifyOtpDto = Voltyks.Core.DTOs.SmsEgyptDTOs.VerifyOtpDto;
using System.Runtime.InteropServices;
using Voltyks.Core.DTOs;
using Microsoft.IdentityModel.Tokens;
using Voltyks.Application;
using Voltyks.Persistence.Entities;
using System.Security.Claims;
using Voltyks.Core.DTOs.ChargerRequest;




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

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            var result = await serviceManager.AuthService.LoginAsync(loginDTO);

            if (!result.Status)
                return Unauthorized(result); // أو BadRequest حسب نوع الخطأ

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            var result = await serviceManager.AuthService.RegisterAsync(registerDTO);

            if (!result.Status)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshJwtToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var result = await serviceManager.AuthService.RefreshJwtTokenAsync(refreshTokenDto);

            if (!result.Status)
            {
                return Unauthorized(result); // أو BadRequest(result) حسب نوع الخطأ الراجع
            }

            return Ok(result);
        }

        [HttpPost("CheckPhoneNumberExists")]
        public async Task<IActionResult> CheckPhoneNumberExists([FromBody] PhoneNumberDto phoneNumberDto)
        {
            if (string.IsNullOrWhiteSpace(phoneNumberDto?.PhoneNumber))
                return BadRequest(new ApiResponse<string>(ErrorMessages.PhoneRequired, false));

            var result = await serviceManager.AuthService.CheckPhoneNumberExistsAsync(phoneNumberDto);

            if (!result.Status)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("CheckEmailExists")]
        public async Task<IActionResult> CheckEmailExists([FromBody] EmailDto emailDto)
        {
            if (string.IsNullOrWhiteSpace(emailDto?.Email))
                return BadRequest(new ApiResponse<string>(ErrorMessages.EmailRequired, false));

            var result = await serviceManager.AuthService.CheckEmailExistsAsync(emailDto);

            if (!result.Status)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] TokenDto tokenDto)
        {


            var result = await serviceManager.AuthService.LogoutAsync(tokenDto);

            if (!result.Status)
                return Unauthorized(result); // أو BadRequest حسب نوع الخطأ

            return Ok(result);
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

            var response = await serviceManager.SmsEgyptService.ResetPasswordAsync(model);

            if (!response.Status)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("GetProfileDetails")]
        public async Task<IActionResult> GetMyDetails()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // من الـ JWT
            var result = await serviceManager.AuthService.GetUserDetailsAsync(userId);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpPut("toggle-availability")]
        [Authorize]
        public async Task<IActionResult> ToggleAvailability()
        {
            var result = await serviceManager.AuthService.ToggleUserAvailabilityAsync();
            return Ok(result);
        }

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

        [HttpGet("getUsersRequests")]
        public async Task<ActionResult<ApiResponse<List<ChargingRequestDetailsDto>>>> GetUserRequests()
        {
            var resp = await serviceManager.AuthService.GetChargerRequestsAsync();
            return Ok(resp);
        }

        [HttpPatch("toggle-banned")]
        public async Task<IActionResult> ToggleMyBan(CancellationToken ct)
           => Ok(await serviceManager.AuthService.ToggleCurrentUserBanAsync(ct));
        [HttpGet("wallet")]
        public async Task<IActionResult> GetMyWallet(CancellationToken ct)
            => Ok(await serviceManager.AuthService.GetMyWalletAsync(ct));

        [HttpPut("wallet/reset")]
        public async Task<IActionResult> ResetMyWallet(CancellationToken ct)
            => Ok(await serviceManager.AuthService.ResetMyWalletAsync(ct));

        [HttpPost("wallet/deduct-fees/{requestId}")]
        public async Task<IActionResult> DeductFeesFromWallet(int requestId, CancellationToken ct)
            => Ok(await serviceManager.AuthService.DeductFeesFromWalletAsync(requestId, ct));

    }
}
