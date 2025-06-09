using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.ModelDTOs;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.Interfaces
{
    public interface IModelService
    {
        Task<ApiResponse<IEnumerable<ModelDto>>> GetModelsByBrandIdAsync(int brandId);
        Task<ApiResponse<IEnumerable<int>>> GetYearsByModelIdAsync(int modelId);

        
    }

}
