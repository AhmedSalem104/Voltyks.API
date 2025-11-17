using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/brands")]
    public class AdminBrandsController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminBrandsController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/brands (Read-only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBrands(CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.GetBrandsAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/models?brandId= (Read-only)
        /// </summary>
        [HttpGet("models")]
        public async Task<IActionResult> GetModels(
            [FromQuery] int? brandId = null,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.GetModelsAsync(brandId, ct);
            return Ok(result);
        }
    }
}
