using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.Store;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Store.Categories;
using Voltyks.Core.DTOs.Store.Products;
using Voltyks.Core.DTOs.Store.Reservations;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main.Store;

namespace Voltyks.Application.Services.Store
{
    public class StoreService : IStoreService
    {
        private readonly VoltyksDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StoreService(VoltyksDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        #region Categories

        public async Task<ApiResponse<List<StoreCategoryListDto>>> GetCategoriesAsync(CancellationToken ct = default)
        {
            try
            {
                var categories = await _context.StoreCategories
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted && c.Status != "hidden")
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => new StoreCategoryListDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug,
                        Status = c.Status,
                        Icon = c.Icon,
                        PlaceholderMessage = c.PlaceholderMessage,
                        ProductCount = c.Products.Count(p => !p.IsDeleted && p.Status == "active")
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<StoreCategoryListDto>>(categories, "Categories retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<StoreCategoryListDto>>(
                    message: "Failed to retrieve categories",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        #endregion

        #region Products

        public async Task<ApiResponse<PagedResult<StoreProductListDto>>> GetProductsAsync(
            int? categoryId,
            string? search,
            PaginationParams pagination,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.StoreProducts
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => !p.IsDeleted && p.Status != "hidden");

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
                }

                var totalCount = await query.CountAsync(ct);

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .Select(p => new StoreProductListDto
                    {
                        Id = p.Id,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category != null ? p.Category.Name : "",
                        Name = p.Name,
                        Slug = p.Slug,
                        Price = p.Price,
                        Currency = p.Currency,
                        ThumbnailImage = GetFirstImage(p.ImagesJson),
                        Status = p.Status,
                        IsReservable = p.IsReservable
                    })
                    .ToListAsync(ct);

                var result = new PagedResult<StoreProductListDto>(products, totalCount, pagination.PageNumber, pagination.PageSize);
                return new ApiResponse<PagedResult<StoreProductListDto>>(result, "Products retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<StoreProductListDto>>(
                    message: "Failed to retrieve products",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<StoreProductDto>> GetProductByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.Status != "hidden", ct);

                if (product == null)
                {
                    return new ApiResponse<StoreProductDto>("Product not found", false);
                }

                var dto = MapToProductDto(product);
                return new ApiResponse<StoreProductDto>(dto, "Product retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<StoreProductDto>(
                    message: "Failed to retrieve product",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<StoreProductDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Slug == slug && !p.IsDeleted && p.Status != "hidden", ct);

                if (product == null)
                {
                    return new ApiResponse<StoreProductDto>("Product not found", false);
                }

                var dto = MapToProductDto(product);
                return new ApiResponse<StoreProductDto>(dto, "Product retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<StoreProductDto>(
                    message: "Failed to retrieve product",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        #endregion

        #region Reservations

        public async Task<ApiResponse<ReservationDto>> CreateReservationAsync(CreateReservationDto dto, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new ApiResponse<ReservationDto>("User not authenticated", false);
                }

                // Check if product exists and is reservable
                var product = await _context.StoreProducts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == dto.ProductId && !p.IsDeleted && p.Status == "active" && p.IsReservable, ct);

                if (product == null)
                {
                    return new ApiResponse<ReservationDto>("Product not found or not available for reservation", false);
                }

                // Check if user already has an active reservation for this product
                var existingReservation = await _context.StoreReservations
                    .AnyAsync(r => r.UserId == userId && r.ProductId == dto.ProductId && r.Status != "cancelled", ct);

                if (existingReservation)
                {
                    return new ApiResponse<ReservationDto>("You already have an active reservation for this product", false);
                }

                var reservation = new StoreReservation
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * dto.Quantity,
                    Status = "pending",
                    PaymentStatus = "unpaid",
                    DeliveryStatus = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StoreReservations.Add(reservation);
                await _context.SaveChangesAsync(ct);

                var resultDto = new ReservationDto
                {
                    Id = reservation.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductThumbnail = GetFirstImage(product.ImagesJson),
                    Quantity = reservation.Quantity,
                    UnitPrice = reservation.UnitPrice,
                    TotalPrice = reservation.TotalPrice,
                    Currency = product.Currency,
                    Status = reservation.Status,
                    PaymentStatus = reservation.PaymentStatus,
                    DeliveryStatus = reservation.DeliveryStatus,
                    CreatedAt = reservation.CreatedAt
                };

                return new ApiResponse<ReservationDto>(resultDto, "Reservation created successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ReservationDto>(
                    message: "Failed to create reservation",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<MyReservationDto>>> GetMyReservationsAsync(CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new ApiResponse<List<MyReservationDto>>("User not authenticated", false);
                }

                var reservations = await _context.StoreReservations
                    .AsNoTracking()
                    .Include(r => r.Product)
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new MyReservationDto
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        ProductName = r.Product != null ? r.Product.Name : "",
                        ProductThumbnail = r.Product != null ? GetFirstImage(r.Product.ImagesJson) : null,
                        Quantity = r.Quantity,
                        UnitPrice = r.UnitPrice,
                        TotalPrice = r.TotalPrice,
                        Currency = r.Product != null ? r.Product.Currency : "EGP",
                        Status = r.Status,
                        PaymentStatus = r.PaymentStatus,
                        DeliveryStatus = r.DeliveryStatus,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<MyReservationDto>>(reservations, "Reservations retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<MyReservationDto>>(
                    message: "Failed to retrieve reservations",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<ReservationDto>> UpdateReservationAsync(int id, UpdateReservationDto dto, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new ApiResponse<ReservationDto>("User not authenticated", false);
                }

                var reservation = await _context.StoreReservations
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

                if (reservation == null)
                {
                    return new ApiResponse<ReservationDto>("Reservation not found", false);
                }

                if (reservation.Status != "pending")
                {
                    return new ApiResponse<ReservationDto>("Cannot update reservation after it has been processed", false);
                }

                reservation.Quantity = dto.Quantity;
                reservation.TotalPrice = reservation.UnitPrice * dto.Quantity;
                reservation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                var resultDto = new ReservationDto
                {
                    Id = reservation.Id,
                    ProductId = reservation.ProductId,
                    ProductName = reservation.Product?.Name ?? "",
                    ProductThumbnail = GetFirstImage(reservation.Product?.ImagesJson),
                    Quantity = reservation.Quantity,
                    UnitPrice = reservation.UnitPrice,
                    TotalPrice = reservation.TotalPrice,
                    Currency = reservation.Product?.Currency ?? "EGP",
                    Status = reservation.Status,
                    PaymentStatus = reservation.PaymentStatus,
                    DeliveryStatus = reservation.DeliveryStatus,
                    CreatedAt = reservation.CreatedAt
                };

                return new ApiResponse<ReservationDto>(resultDto, "Reservation updated successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ReservationDto>(
                    message: "Failed to update reservation",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> CancelReservationAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new ApiResponse<bool>("User not authenticated", false);
                }

                var reservation = await _context.StoreReservations
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

                if (reservation == null)
                {
                    return new ApiResponse<bool>("Reservation not found", false);
                }

                if (reservation.Status == "completed")
                {
                    return new ApiResponse<bool>("Cannot cancel a completed reservation", false);
                }

                reservation.Status = "cancelled";
                reservation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return new ApiResponse<bool>(true, "Reservation cancelled successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to cancel reservation",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        #endregion

        #region Private Helpers

        private static string? GetFirstImage(string? imagesJson)
        {
            if (string.IsNullOrEmpty(imagesJson)) return null;
            try
            {
                var images = JsonSerializer.Deserialize<List<string>>(imagesJson);
                return images?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static List<string> ParseImages(string? imagesJson)
        {
            if (string.IsNullOrEmpty(imagesJson)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(imagesJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static Dictionary<string, string>? ParseSpecifications(string? specificationsJson)
        {
            if (string.IsNullOrEmpty(specificationsJson)) return null;
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(specificationsJson);
            }
            catch
            {
                return null;
            }
        }

        private static StoreProductDto MapToProductDto(StoreProduct product)
        {
            return new StoreProductDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? "",
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                Price = product.Price,
                Currency = product.Currency,
                Images = ParseImages(product.ImagesJson),
                Specifications = ParseSpecifications(product.SpecificationsJson),
                Status = product.Status,
                IsReservable = product.IsReservable
            };
        }

        #endregion
    }
}
