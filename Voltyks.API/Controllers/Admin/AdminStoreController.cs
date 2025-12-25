using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.AdminControlDashboard;
using Voltyks.Application.Interfaces.ImageUpload;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.ImageUpload;
using Voltyks.Core.DTOs.Store.Admin;

namespace Voltyks.API.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/store")]
    public class AdminStoreController : ControllerBase
    {
        private readonly IAdminServiceManager _adminServiceManager;
        private readonly IProductImageService _productImageService;

        public AdminStoreController(
            IAdminServiceManager adminServiceManager,
            IProductImageService productImageService)
        {
            _adminServiceManager = adminServiceManager;
            _productImageService = productImageService;
        }

        #region Category Endpoints

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories(
            [FromQuery] bool withTrashed = false,
            [FromQuery] bool onlyTrashed = false,
            [FromQuery] string? status = null,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.GetCategoriesAsync(withTrashed, onlyTrashed, status, ct);
            return Ok(result);
        }

        [HttpGet("categories/{id}")]
        public async Task<IActionResult> GetCategoryById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.GetCategoryByIdAsync(id, ct);
            return Ok(result);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.CreateCategoryAsync(dto, ct);
            return Ok(result);
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.UpdateCategoryAsync(id, dto, ct);
            return Ok(result);
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.DeleteCategoryAsync(id, ct);
            return Ok(result);
        }

        [HttpPost("categories/{id}/restore")]
        public async Task<IActionResult> RestoreCategory(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.RestoreCategoryAsync(id, ct);
            return Ok(result);
        }

        [HttpDelete("categories/{id}/force")]
        public async Task<IActionResult> ForceDeleteCategory(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.ForceDeleteCategoryAsync(id, ct);
            return Ok(result);
        }

        #endregion

        #region Product Endpoints

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId = null,
            [FromQuery] string? status = null,
            [FromQuery] bool withTrashed = false,
            [FromQuery] bool onlyTrashed = false,
            CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.GetProductsAsync(categoryId, status, withTrashed, onlyTrashed, ct);
            return Ok(result);
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProductById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.GetProductByIdAsync(id, ct);
            return Ok(result);
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.CreateProductAsync(dto, ct);
            return Ok(result);
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.UpdateProductAsync(id, dto, ct);
            return Ok(result);
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.DeleteProductAsync(id, ct);
            return Ok(result);
        }

        [HttpPost("products/{id}/restore")]
        public async Task<IActionResult> RestoreProduct(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.RestoreProductAsync(id, ct);
            return Ok(result);
        }

        [HttpDelete("products/{id}/force")]
        public async Task<IActionResult> ForceDeleteProduct(int id, CancellationToken ct = default)
        {
            // First, cleanup images folder (before product is deleted from DB)
            await _productImageService.DeleteAllProductImagesAsync(id, ct);

            // Then delete the product from database
            var result = await _adminServiceManager.AdminStoreService.ForceDeleteProductAsync(id, ct);
            return Ok(result);
        }

        #endregion

        #region Product Image Endpoints

        [HttpPost("products/{id}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProductImages(
            int id,
            [FromForm] UploadProductImagesDto dto,
            CancellationToken ct = default)
        {
            var result = await _productImageService.UploadImagesAsync(id, dto.Images, ct);
            return Ok(result);
        }

        [HttpDelete("products/{id}/images")]
        public async Task<IActionResult> DeleteProductImage(
            int id,
            [FromBody] DeleteProductImageDto dto,
            CancellationToken ct = default)
        {
            var result = await _productImageService.DeleteImageAsync(id, dto.ImagePath, ct);
            return Ok(result);
        }

        [HttpDelete("products/{id}/images/all")]
        public async Task<IActionResult> DeleteAllProductImages(int id, CancellationToken ct = default)
        {
            var result = await _productImageService.DeleteAllProductImagesAsync(id, ct);
            return Ok(result);
        }

        #endregion

        #region Reservation Endpoints

        [HttpGet("reservations")]
        public async Task<IActionResult> GetReservations(
            [FromQuery] string? status = null,
            [FromQuery] string? paymentStatus = null,
            [FromQuery] string? deliveryStatus = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var pagination = new PaginationParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _adminServiceManager.AdminStoreService.GetReservationsAsync(
                status, paymentStatus, deliveryStatus, fromDate, toDate, search, pagination, ct);
            return Ok(result);
        }

        [HttpGet("reservations/{id}")]
        public async Task<IActionResult> GetReservationById(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.GetReservationByIdAsync(id, ct);
            return Ok(result);
        }

        [HttpPut("reservations/{id}/contact")]
        public async Task<IActionResult> RecordContact(int id, [FromBody] RecordContactDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.RecordContactAsync(id, dto, ct);
            return Ok(result);
        }

        [HttpPut("reservations/{id}/payment")]
        public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordPaymentDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.RecordPaymentAsync(id, dto, ct);
            return Ok(result);
        }

        [HttpPut("reservations/{id}/delivery")]
        public async Task<IActionResult> RecordDelivery(int id, [FromBody] RecordDeliveryDto dto, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.RecordDeliveryAsync(id, dto, ct);
            return Ok(result);
        }

        [HttpPut("reservations/{id}/complete")]
        public async Task<IActionResult> CompleteReservation(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.CompleteReservationAsync(id, ct);
            return Ok(result);
        }

        [HttpPut("reservations/{id}/cancel")]
        public async Task<IActionResult> CancelReservation(int id, CancellationToken ct = default)
        {
            var result = await _adminServiceManager.AdminStoreService.CancelReservationAsync(id, ct);
            return Ok(result);
        }

        #endregion
    }
}
