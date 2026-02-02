using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Process;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;
using Voltyks.Persistence.Entities.Main;

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

        /// <summary>
        /// Unified termination path for all process exits.
        /// - Idempotent: safe to call even if process already terminal
        /// - Sets Process.Status and ChargingRequest.Status
        /// - Cleans up CurrentActivities for both users
        /// - Resets IsAvailable if no other activities
        /// - Sends Process_Terminated notification to both users
        /// </summary>
        Task TerminateProcessAsync(
            int processId,
            ProcessStatus targetStatus,
            string terminationReason,
            string? actorUserId = null,
            CancellationToken ct = default);
    }
}
