using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Persistence.Entities.Identity;


namespace Voltyks.Application
{
    public interface IAuthService
    {
        Task<UserLoginResultDto> LoginAsync(LoginDTO model);
        Task<UserRegisterationResultDto> RegisterAsync(RegisterDTO model);
        Task LogoutAsync(TokenDto tokenDto);
        Task<string> RefreshJwtTokenAsync(RefreshTokenDto refreshTokenDto);
        Task CheckEmailExistsAsync(EmailDto emailDto);
        Task CheckPhoneNumberExistsAsync(PhoneNumberDto phoneNumberDto);
        Task<UserLoginResultDto> ExternalLoginAsync(ExternalAuthDto model);

      
    }


}
