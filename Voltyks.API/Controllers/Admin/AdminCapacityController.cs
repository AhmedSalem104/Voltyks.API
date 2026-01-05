using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.Capacity;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/capacity")]
    public class AdminCapacityController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminCapacityController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/capacity - Get all capacities
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminCapacityService.GetAllAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/capacity/{id} - Get capacity by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminCapacityService.GetByIdAsync(id, ct);
            if (!result.Status)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/capacity - Create new capacity
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCapacityDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminCapacityService.CreateAsync(dto, ct);
            if (!result.Status)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// PUT /api/admin/capacity/{id} - Update capacity
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCapacityDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminCapacityService.UpdateAsync(id, dto, ct);
            if (!result.Status)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/admin/capacity/{id} - Delete capacity
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminCapacityService.DeleteAsync(id, ct);
            if (!result.Status)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
