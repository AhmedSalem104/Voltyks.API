using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces.VehicleAdditionRequest;
using Voltyks.Core.DTOs.VehicleAdditionRequests;

namespace Voltyks.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/vehicle-addition-requests")]
    public class VehicleAdditionRequestsController : ControllerBase
    {
        private readonly IVehicleAdditionRequestService _service;

        public VehicleAdditionRequestsController(IVehicleAdditionRequestService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST /api/vehicle-addition-requests
        /// Submit a new vehicle addition request.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateVehicleAdditionRequestDto dto,
            CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _service.CreateAsync(userId, dto, ct);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// GET /api/vehicle-addition-requests/my
        /// Get current user's vehicle addition requests.
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRequests(CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _service.GetMyRequestsAsync(userId, ct);
            return Ok(result);
        }
    }
}
