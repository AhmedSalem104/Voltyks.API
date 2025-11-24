using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.Brands;

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

        #region Brand Endpoints

        /// <summary>
        /// GET /api/admin/brands - Get all brands
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBrands(CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.GetBrandsAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/brands/{id} - Get brand by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrandById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.GetBrandByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/brands - Create new brand
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBrand(
            [FromBody] CreateBrandDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.CreateBrandAsync(dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// PUT /api/admin/brands/{id} - Update brand
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBrand(
            int id,
            [FromBody] UpdateBrandDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.UpdateBrandAsync(id, dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/admin/brands/{id} - Delete brand
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.DeleteBrandAsync(id, ct);
            return Ok(result);
        }

        #endregion

        #region Model Endpoints

        /// <summary>
        /// GET /api/admin/brands/models?brandId= - Get all models (optionally filtered by brand)
        /// </summary>
        [HttpGet("models")]
        public async Task<IActionResult> GetModels(
            [FromQuery] int? brandId = null,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.GetModelsAsync(brandId, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/brands/models/{id} - Get model by ID
        /// </summary>
        [HttpGet("models/{id}")]
        public async Task<IActionResult> GetModelById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.GetModelByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/brands/models - Create new model
        /// </summary>
        [HttpPost("models")]
        public async Task<IActionResult> CreateModel(
            [FromBody] CreateModelDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.CreateModelAsync(dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// PUT /api/admin/brands/models/{id} - Update model
        /// </summary>
        [HttpPut("models/{id}")]
        public async Task<IActionResult> UpdateModel(
            int id,
            [FromBody] UpdateModelDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.UpdateModelAsync(id, dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/admin/brands/models/{id} - Delete model
        /// </summary>
        [HttpDelete("models/{id}")]
        public async Task<IActionResult> DeleteModel(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminBrandsService.DeleteModelAsync(id, ct);
            return Ok(result);
        }

        #endregion
    }
}
