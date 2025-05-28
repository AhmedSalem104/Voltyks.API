using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.SmsBeOnDTOs;

namespace Voltyks.Application.Interfaces
{
    public interface ISmsBeOnService
    {
        Task<ApiResponse<string>> SendOtpAsync(SendOtpDto dto);
        Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto);
    }
}
