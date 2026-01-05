using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.AdminControlDashboard.Dtos.ComplaintCategories;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/complaint-categories")]
    public class AdminComplaintCategoriesController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;

        public AdminComplaintCategoriesController(IAdminServiceManager adminServiceManager)
        {
            _adminServiceManager = adminServiceManager;
        }

        /// <summary>
        /// GET /api/admin/complaint-categories - Get all categories
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories(
            [FromQuery] bool includeDeleted = false,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintCategoriesService.GetCategoriesAsync(includeDeleted, ct);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/admin/complaint-categories/{id} - Get category by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintCategoriesService.GetCategoryByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/admin/complaint-categories - Create new category
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCategory(
            [FromBody] CreateComplaintCategoryDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintCategoriesService.CreateCategoryAsync(dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// PUT /api/admin/complaint-categories/{id} - Update category
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] UpdateComplaintCategoryDto dto,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintCategoriesService.UpdateCategoryAsync(id, dto, ct);
            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/admin/complaint-categories/{id} - Soft delete category
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintCategoriesService.DeleteCategoryAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// PATCH /api/admin/complaint-categories/{id}/restore - Restore deleted category
        /// </summary>
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreCategory(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminComplaintCategoriesService.RestoreCategoryAsync(id, ct);
            return Ok(result);
        }
    }
}
