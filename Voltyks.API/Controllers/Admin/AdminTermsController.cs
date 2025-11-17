using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.Terms;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/terms")]
    public class AdminTermsController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminTermsController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/terms?lang=
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTerms(
            [FromQuery] string lang = "en",
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminTermsService.GetTermsAsync(lang, ct);
            return Ok(result);
        }

        /// <summary>
        /// PUT /api/admin/terms
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateTerms(
            [FromBody] UpdateTermsDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminTermsService.UpdateTermsAsync(dto, ct);
            return Ok(result);
        }
    }
}
