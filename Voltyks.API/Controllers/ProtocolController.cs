using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Persistence.Data;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProtocolController : ControllerBase
    {
        private readonly VoltyksDbContext _context;

        public ProtocolController(VoltyksDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/protocol - Get list of all protocols
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProtocols(CancellationToken ct = default)
        {
            var protocols = await _context.Protocols
                .AsNoTracking()
                .Select(p => new ProtocolDto { Id = p.Id, Name = p.Name })
                .ToListAsync(ct);

            return Ok(new ApiResponse<List<ProtocolDto>>(protocols, "Protocols retrieved successfully", true));
        }
    }
}
