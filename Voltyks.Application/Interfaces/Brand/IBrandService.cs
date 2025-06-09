using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.BrandsDTOs;

namespace Voltyks.Application.Interfaces.Brand
{
    public interface IBrandService
    {
        Task<ApiResponse<IEnumerable<BrandReadDto>>> GetAllAsync();
    }


}
