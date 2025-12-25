using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Store.Categories;
using Voltyks.Core.DTOs.Store.Products;
using Voltyks.Core.DTOs.Store.Reservations;

namespace Voltyks.Application.Interfaces.Store
{
    public interface IStoreService
    {
        // Categories
        Task<ApiResponse<List<StoreCategoryListDto>>> GetCategoriesAsync(CancellationToken ct = default);

        // Products
        Task<ApiResponse<PagedResult<StoreProductListDto>>> GetProductsAsync(
            int? categoryId,
            string? search,
            PaginationParams pagination,
            CancellationToken ct = default);

        Task<ApiResponse<StoreProductDto>> GetProductByIdAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<StoreProductDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default);

        // Reservations
        Task<ApiResponse<ReservationDto>> CreateReservationAsync(CreateReservationDto dto, CancellationToken ct = default);

        Task<ApiResponse<List<MyReservationDto>>> GetMyReservationsAsync(CancellationToken ct = default);

        Task<ApiResponse<ReservationDto>> UpdateReservationAsync(int id, UpdateReservationDto dto, CancellationToken ct = default);

        Task<ApiResponse<bool>> CancelReservationAsync(int id, CancellationToken ct = default);
    }
}
