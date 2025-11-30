using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces.Terms;
using Voltyks.Core.DTOs;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/terms")]
    public class TermsController : ControllerBase
    {
        private readonly ITermsService _terms;

        public TermsController(ITermsService terms)
        {
            _terms = terms;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string lang = "en", [FromQuery] int? version = null, CancellationToken ct = default)
        {
            var result = await _terms.GetAsync(lang, version, ct);
            if (result is null)
            {
                return NotFound(new ApiResponse<object>(
                    message: $"Terms not found for language '{lang}'" + (version.HasValue ? $" and version {version}" : ""),
                    status: false
                ));
            }

            // ETag / Cache (اختياري)
            var etag = $"W/\"terms-v{result.Version}-{result.PublishedAt:yyyyMMddHHmmss}-{result.Lang}\"";
            Response.Headers["ETag"] = etag;
            Response.Headers["Cache-Control"] = "public, max-age=3600";
            if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm.ToString() == etag)
                return StatusCode(StatusCodes.Status304NotModified);

            return Ok(new ApiResponse<object>(result, "Terms retrieved successfully", true));
        }
    }
}
