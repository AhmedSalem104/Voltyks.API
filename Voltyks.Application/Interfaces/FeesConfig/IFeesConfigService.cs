using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.FeesConfig;
using Voltyks.Core.DTOs;

namespace Voltyks.Application.Interfaces.FeesConfig
{
    public interface IFeesConfigService
    {
        Task<ApiResponse<FeesConfigDto>> GetAsync();
        Task<ApiResponse<FeesConfigDto>> UpdateAsync(FeesConfigUpdateDto dto);
    }
}
