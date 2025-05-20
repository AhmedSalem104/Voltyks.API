using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.AuthDTOs;


namespace Voltyks.Application
{
    public interface IAuthService
    {
      
        Task<UserLoginResultDto> LoginAsync(LoginDTO model);
        Task<UserRegisterationResultDto> RegisterAsync(RegisterDTO model);
        Task LogoutAsync(TokenDto tokenDto );
        Task<string> RefreshJwtTokenAsync(RefreshTokenDto refreshTokenDto);
        Task SendOtpAsync(PhoneNumberDto phoneNumberDto);
        Task<bool> VerifyOtpAsync(VerifyOtpDto verifyOtpDto);
        Task ForgotPasswordAsync(PhoneNumberDto phoneNumberDto);
        Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task CheckEmailExistsAsync(EmailDto emailDto );
        Task CheckPhoneNumberExistsAsync(PhoneNumberDto phoneNumberDto);
        Task<UserLoginResultDto> ExternalLoginAsync(ExternalAuthDto model);
        Task SendOtpUsingTwilioAsync(PhoneNumberDto phoneNumberDto);

    }

}
