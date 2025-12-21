using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.DTOs.ChargerRequest;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Complaints;
using Voltyks.Persistence.Entities.Identity;


namespace Voltyks.Application.Interfaces.Auth
{
    public interface IAuthService
    {


        Task<ApiResponse<UserLoginResultDto>> LoginAsync(LoginDTO model);
        Task<ApiResponse<UserRegisterationResultDto>> RegisterAsync(RegisterDTO model);
        Task<ApiResponse<List<string>>> LogoutAsync(TokenDto tokenDto);
        Task<ApiResponse<string>> RefreshJwtTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<ApiResponse<string>> RefreshJwtTokenFromCookiesAsync();
        Task<ApiResponse<List<string>>> CheckEmailExistsAsync(EmailDto emailDto);
        Task<ApiResponse<List<string>>> CheckPhoneNumberExistsAsync(PhoneNumberDto phoneNumberDto);
        Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(string userId);
        Task<ApiResponse<bool>> ToggleUserAvailabilityAsync();

        Task<ApiResponse<object>> GetChargerRequestsAsync(PaginationParams? paginationParams = null, CancellationToken ct = default);

        Task<ApiResponse<object>> ToggleCurrentUserBanAsync(CancellationToken ct = default);

        Task<ApiResponse<double?>> GetMyWalletAsync(CancellationToken ct = default);

        Task<ApiResponse<double?>> ResetMyWalletAsync(CancellationToken ct = default);

        Task<ApiResponse<object>> DeductFeesFromWalletAsync(int requestId, CancellationToken ct = default);

        Task<ApiResponse<object>> CreateGeneralComplaintAsync(CreateGeneralComplaintDto dto, CancellationToken ct = default);
        Task<ApiResponse<CanSubmitComplaintDto>> CanSubmitComplaintAsync(CancellationToken ct = default);

        Task<ApiResponse<object>> CheckPasswordAsync(CheckPasswordDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> RequestEmailChangeAsync(RequestEmailChangeDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> VerifyEmailChangeAsync(VerifyEmailChangeDto dto, CancellationToken ct = default);

        // Simple change email/password (no OTP required)
        Task<ApiResponse<object>> ChangeEmailAsync(ChangeEmailDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct = default);

    }


}
