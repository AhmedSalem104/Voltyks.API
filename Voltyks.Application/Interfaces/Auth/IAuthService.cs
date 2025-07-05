using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Persistence.Entities.Identity;


namespace Voltyks.Application.Interfaces.Auth
{
    public interface IAuthService
    {


        Task<ApiResponse<UserLoginResultDto>> LoginAsync(LoginDTO model);
        Task<ApiResponse<UserRegisterationResultDto>> RegisterAsync(RegisterDTO model);
        Task<ApiResponse<List<string>>> LogoutAsync(TokenDto tokenDto);
        Task<ApiResponse<string>> RefreshJwtTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<ApiResponse<List<string>>> CheckEmailExistsAsync(EmailDto emailDto);
        Task<ApiResponse<List<string>>> CheckPhoneNumberExistsAsync(PhoneNumberDto phoneNumberDto);
        Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(string userId);




    }


}
