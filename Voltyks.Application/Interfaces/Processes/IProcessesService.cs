using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Process;
using Voltyks.Core.DTOs;

namespace Voltyks.Application.Interfaces.Processes
{
    public interface IProcessesService
    {
        Task<ApiResponse<object>> ConfirmByVehicleOwnerAsync(ConfirmByVehicleOwnerDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> OwnerDecisionAsync(OwnerDecisionDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> SubmitRatingAsync(SubmitRatingDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> GetMyActivitiesAsync(CancellationToken ct = default);
    }
}
