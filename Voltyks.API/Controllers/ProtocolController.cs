using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

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

        /// <summary>
        /// GET /api/protocol/{id} - Get protocol by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProtocolById(int id, CancellationToken ct = default)
        {
            var protocol = await _context.Protocols
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProtocolDto { Id = p.Id, Name = p.Name })
                .FirstOrDefaultAsync(ct);

            if (protocol == null)
                return Ok(new ApiResponse<ProtocolDto>("Protocol not found", false));

            return Ok(new ApiResponse<ProtocolDto>(protocol, "Protocol retrieved successfully", true));
        }

        /// <summary>
        /// POST /api/protocol - Create new protocol
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProtocol([FromBody] CreateProtocolDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Ok(new ApiResponse<ProtocolDto>("Name is required", false));

            var protocol = new Protocol { Name = dto.Name };
            _context.Protocols.Add(protocol);
            await _context.SaveChangesAsync(ct);

            var result = new ProtocolDto { Id = protocol.Id, Name = protocol.Name };
            return Ok(new ApiResponse<ProtocolDto>(result, "Protocol created successfully", true));
        }

        /// <summary>
        /// PUT /api/protocol/{id} - Update protocol
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProtocol(int id, [FromBody] CreateProtocolDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Ok(new ApiResponse<ProtocolDto>("Name is required", false));

            var protocol = await _context.Protocols.FindAsync(new object[] { id }, ct);
            if (protocol == null)
                return Ok(new ApiResponse<ProtocolDto>("Protocol not found", false));

            protocol.Name = dto.Name;
            await _context.SaveChangesAsync(ct);

            var result = new ProtocolDto { Id = protocol.Id, Name = protocol.Name };
            return Ok(new ApiResponse<ProtocolDto>(result, "Protocol updated successfully", true));
        }

        /// <summary>
        /// DELETE /api/protocol/{id} - Delete protocol
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProtocol(int id, CancellationToken ct = default)
        {
            var protocol = await _context.Protocols.FindAsync(new object[] { id }, ct);
            if (protocol == null)
                return Ok(new ApiResponse<object>("Protocol not found", false));

            _context.Protocols.Remove(protocol);
            await _context.SaveChangesAsync(ct);

            return Ok(new ApiResponse<object>(null, "Protocol deleted successfully", true));
        }
    }

    public class CreateProtocolDto
    {
        public string Name { get; set; } = default!;
    }
}
