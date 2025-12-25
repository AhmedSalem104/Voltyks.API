using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Store.Admin;

namespace Voltyks.AdminControlDashboard.Interfaces
{
    public interface IAdminStoreService
    {
        // Categories CRUD
        Task<ApiResponse<List<AdminCategoryDto>>> GetCategoriesAsync(
            bool withTrashed = false,
            bool onlyTrashed = false,
            string? status = null,
            CancellationToken ct = default);

        Task<ApiResponse<AdminCategoryDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<AdminCategoryDto>> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct = default);

        Task<ApiResponse<AdminCategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default);

        Task<ApiResponse<bool>> DeleteCategoryAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<bool>> RestoreCategoryAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<bool>> ForceDeleteCategoryAsync(int id, CancellationToken ct = default);

        // Products CRUD
        Task<ApiResponse<List<AdminProductDto>>> GetProductsAsync(
            int? categoryId = null,
            string? status = null,
            bool withTrashed = false,
            bool onlyTrashed = false,
            CancellationToken ct = default);

        Task<ApiResponse<AdminProductDto>> GetProductByIdAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<AdminProductDto>> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default);

        Task<ApiResponse<AdminProductDto>> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken ct = default);

        Task<ApiResponse<bool>> DeleteProductAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<bool>> RestoreProductAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<bool>> ForceDeleteProductAsync(int id, CancellationToken ct = default);

        // Reservations Management
        Task<ApiResponse<PagedResult<AdminReservationDto>>> GetReservationsAsync(
            string? status = null,
            string? paymentStatus = null,
            string? deliveryStatus = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? search = null,
            PaginationParams? pagination = null,
            CancellationToken ct = default);

        Task<ApiResponse<AdminReservationDto>> GetReservationByIdAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<AdminReservationDto>> RecordContactAsync(int id, RecordContactDto dto, CancellationToken ct = default);

        Task<ApiResponse<AdminReservationDto>> RecordPaymentAsync(int id, RecordPaymentDto dto, CancellationToken ct = default);

        Task<ApiResponse<AdminReservationDto>> RecordDeliveryAsync(int id, RecordDeliveryDto dto, CancellationToken ct = default);

        Task<ApiResponse<AdminReservationDto>> CompleteReservationAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<bool>> CancelReservationAsync(int id, CancellationToken ct = default);
    }
}
