using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Process;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.Application.Interfaces.Processes
{
    public interface IProcessesService
    {
        Task<ApiResponse<object>> ConfirmByVehicleOwnerAsync(ConfirmByVehicleOwnerDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> OwnerDecisionAsync(OwnerDecisionDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> SubmitRatingAsync(SubmitRatingDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>> GetMyActivitiesAsync(PaginationParams? paginationParams, CancellationToken ct = default);
        Task<ApiResponse<object>> GetRatingsSummaryAsync(int Id, CancellationToken ct = default);
        Task<ApiResponse<object>> UpdateProcessAsync(UpdateProcessDto dto, CancellationToken ct = default);
        Task<ApiResponse<PendingProcessDto>> GetPendingProcessesAsync(CancellationToken ct = default);


    }
}
