using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Process;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/processes")]
    [Authorize]
    public class ProcessesController : ControllerBase
    {
        private readonly IServiceManager _svc;
        public ProcessesController(IServiceManager svc) => _svc = svc;

        [HttpPost("confirm-by-vehicle-owner")]
        public async Task<IActionResult> ConfirmByVehicleOwner([FromBody] ConfirmByVehicleOwnerDto dto, CancellationToken ct)
            => Ok(await _svc.ProcessesService.ConfirmByVehicleOwnerAsync(dto, ct));

     
        [HttpPost("update-Process")]
        public async Task<IActionResult> UpdateProcess([FromBody] UpdateProcessDto dto, CancellationToken ct)
            => Ok(await _svc.ProcessesService.UpdateProcessAsync(dto, ct));


        [HttpPost("owner-decision")]
        public async Task<IActionResult> OwnerDecision([FromBody] OwnerDecisionDto dto, CancellationToken ct)
            => Ok(await _svc.ProcessesService.OwnerDecisionAsync(dto, ct));

        [HttpPost("submit-rating")]
        public async Task<IActionResult> SubmitRating([FromBody] SubmitRatingDto dto, CancellationToken ct)
            => Ok(await _svc.ProcessesService.SubmitRatingAsync(dto, ct));

        [HttpGet("my-activities")]
        public async Task<IActionResult> MyActivities([FromQuery] PaginationParams? paginationParams, CancellationToken ct)
            => Ok(await _svc.ProcessesService.GetMyActivitiesAsync(paginationParams, ct));

        [HttpPost("Get-Two-Way-Rating")]
        [Authorize]
        public async Task<IActionResult> GetRatingsSummary([FromBody] ProcessIdDTO dto, CancellationToken ct)
            => Ok(await _svc.ProcessesService.GetRatingsSummaryAsync(dto.Id , ct));

        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<PendingProcessesResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingProcesses(CancellationToken ct)
            => Ok(await _svc.ProcessesService.GetPendingProcessesAsync(ct));


    }
}
