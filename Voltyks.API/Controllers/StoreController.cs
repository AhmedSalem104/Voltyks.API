using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Store.Reservations;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/store")]
    public class StoreController(IServiceManager _serviceManager) : ControllerBase
    {
        #region Categories

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories(CancellationToken ct)
        {
            var result = await _serviceManager.StoreService.GetCategoriesAsync(ct);
            return Ok(result);
        }

        #endregion

        #region Products

        /// <summary>
        /// Get products with optional filtering
        /// </summary>
        /// <param name="categoryId">Filter by category ID</param>
        /// <param name="search">Search in product name/description</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20)</param>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId,
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var pagination = new PaginationParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _serviceManager.StoreService.GetProductsAsync(categoryId, search, pagination, ct);
            return Ok(result);
        }

        [HttpGet("GetProductById")]
        public async Task<IActionResult> GetProductById([FromQuery] int id, CancellationToken ct)
        {
            var result = await _serviceManager.StoreService.GetProductByIdAsync(id, ct);
            return Ok(result);
        }

        [HttpGet("GetProductBySlug")]
        public async Task<IActionResult> GetProductBySlug([FromQuery] string slug, CancellationToken ct)
        {
            var result = await _serviceManager.StoreService.GetProductBySlugAsync(slug, ct);
            return Ok(result);
        }

        #endregion

        #region Reservations

        [Authorize]
        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto, CancellationToken ct)
        {
            var result = await _serviceManager.StoreService.CreateReservationAsync(dto, ct);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("my-reservations")]
        public async Task<IActionResult> GetMyReservations(CancellationToken ct)
        {
            var result = await _serviceManager.StoreService.GetMyReservationsAsync(ct);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("UpdateMyReservation")]
        public async Task<IActionResult> UpdateReservation([FromQuery] int id, [FromBody] UpdateReservationDto dto, CancellationToken ct)
        {
            var result = await _serviceManager.StoreService.UpdateReservationAsync(id, dto, ct);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("CancelMyReservation")]
        public async Task<IActionResult> CancelReservation([FromQuery] int id, CancellationToken ct)
        {
            var result = await _serviceManager.StoreService.CancelReservationAsync(id, ct);
            return Ok(result);
        }

        #endregion
    }
}
