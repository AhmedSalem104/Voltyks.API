using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Common;
using Voltyks.Core.DTOs.Store.Admin;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main.Store;

namespace Voltyks.AdminControlDashboard.Services
{
    public class AdminStoreService : IAdminStoreService
    {
        private readonly VoltyksDbContext _context;

        public AdminStoreService(VoltyksDbContext context)
        {
            _context = context;
        }

        #region Categories

        public async Task<ApiResponse<List<AdminCategoryDto>>> GetCategoriesAsync(
            bool withTrashed = false,
            bool onlyTrashed = false,
            string? status = null,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.StoreCategories.AsNoTracking();

                if (onlyTrashed)
                {
                    query = query.Where(c => c.IsDeleted);
                }
                else if (!withTrashed)
                {
                    query = query.Where(c => !c.IsDeleted);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                var categories = await query
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => new AdminCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug,
                        Status = c.Status,
                        SortOrder = c.SortOrder,
                        Icon = c.Icon,
                        PlaceholderMessage = c.PlaceholderMessage,
                        ProductCount = c.Products.Count(p => !p.IsDeleted),
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        IsDeleted = c.IsDeleted,
                        DeletedAt = c.DeletedAt
                    })
                    .ToListAsync(ct);

                return new ApiResponse<List<AdminCategoryDto>>(categories, "Categories retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminCategoryDto>>(
                    message: "Failed to retrieve categories",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminCategoryDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var category = await _context.StoreCategories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

                if (category == null)
                {
                    return new ApiResponse<AdminCategoryDto>("Category not found", false);
                }

                var dto = new AdminCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Status = category.Status,
                    SortOrder = category.SortOrder,
                    Icon = category.Icon,
                    PlaceholderMessage = category.PlaceholderMessage,
                    ProductCount = category.Products.Count(p => !p.IsDeleted),
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    IsDeleted = category.IsDeleted,
                    DeletedAt = category.DeletedAt
                };

                return new ApiResponse<AdminCategoryDto>(dto, "Category retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminCategoryDto>(
                    message: "Failed to retrieve category",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminCategoryDto>> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct = default)
        {
            try
            {
                var slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug;

                // Check for duplicate slug
                var existingSlug = await _context.StoreCategories
                    .AnyAsync(c => c.Slug == slug, ct);

                if (existingSlug)
                {
                    return new ApiResponse<AdminCategoryDto>("Category with this slug already exists", false);
                }

                var category = new StoreCategory
                {
                    Name = dto.Name,
                    Slug = slug,
                    Status = dto.Status,
                    SortOrder = dto.SortOrder,
                    Icon = dto.Icon,
                    PlaceholderMessage = dto.PlaceholderMessage,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StoreCategories.Add(category);
                await _context.SaveChangesAsync(ct);

                var resultDto = new AdminCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Status = category.Status,
                    SortOrder = category.SortOrder,
                    Icon = category.Icon,
                    PlaceholderMessage = category.PlaceholderMessage,
                    ProductCount = 0,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    IsDeleted = category.IsDeleted,
                    DeletedAt = category.DeletedAt
                };

                return new ApiResponse<AdminCategoryDto>(resultDto, "Category created successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminCategoryDto>(
                    message: "Failed to create category",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminCategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default)
        {
            try
            {
                var category = await _context.StoreCategories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

                if (category == null)
                {
                    return new ApiResponse<AdminCategoryDto>("Category not found", false);
                }

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    category.Name = dto.Name;

                if (!string.IsNullOrWhiteSpace(dto.Slug))
                {
                    var existingSlug = await _context.StoreCategories
                        .AnyAsync(c => c.Slug == dto.Slug && c.Id != id, ct);

                    if (existingSlug)
                    {
                        return new ApiResponse<AdminCategoryDto>("Category with this slug already exists", false);
                    }
                    category.Slug = dto.Slug;
                }

                if (!string.IsNullOrWhiteSpace(dto.Status))
                    category.Status = dto.Status;

                if (dto.SortOrder.HasValue)
                    category.SortOrder = dto.SortOrder.Value;

                if (dto.Icon != null)
                    category.Icon = dto.Icon;

                if (dto.PlaceholderMessage != null)
                    category.PlaceholderMessage = dto.PlaceholderMessage;

                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                var resultDto = new AdminCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Status = category.Status,
                    SortOrder = category.SortOrder,
                    Icon = category.Icon,
                    PlaceholderMessage = category.PlaceholderMessage,
                    ProductCount = category.Products.Count(p => !p.IsDeleted),
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    IsDeleted = category.IsDeleted,
                    DeletedAt = category.DeletedAt
                };

                return new ApiResponse<AdminCategoryDto>(resultDto, "Category updated successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminCategoryDto>(
                    message: "Failed to update category",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> DeleteCategoryAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var category = await _context.StoreCategories
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

                if (category == null)
                {
                    return new ApiResponse<bool>("Category not found", false);
                }

                category.IsDeleted = true;
                category.DeletedAt = DateTime.UtcNow;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return new ApiResponse<bool>(true, "Category deleted successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to delete category",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> RestoreCategoryAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var category = await _context.StoreCategories
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted, ct);

                if (category == null)
                {
                    return new ApiResponse<bool>("Category not found or not deleted", false);
                }

                category.IsDeleted = false;
                category.DeletedAt = null;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return new ApiResponse<bool>(true, "Category restored successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to restore category",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> ForceDeleteCategoryAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var category = await _context.StoreCategories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

                if (category == null)
                {
                    return new ApiResponse<bool>("Category not found", false);
                }

                if (category.Products.Any())
                {
                    return new ApiResponse<bool>("Cannot permanently delete category with products. Delete or move products first.", false);
                }

                _context.StoreCategories.Remove(category);
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<bool>(true, "Category permanently deleted", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to permanently delete category",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        #endregion

        #region Products

        public async Task<ApiResponse<List<AdminProductDto>>> GetProductsAsync(
            int? categoryId = null,
            string? status = null,
            bool withTrashed = false,
            bool onlyTrashed = false,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.StoreProducts
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Reservations)
                    .AsQueryable();

                if (onlyTrashed)
                {
                    query = query.Where(p => p.IsDeleted);
                }
                else if (!withTrashed)
                {
                    query = query.Where(p => !p.IsDeleted);
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(ct);

                var productDtos = products.Select(p => new AdminProductDto
                {
                    Id = p.Id,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name ?? "",
                    Name = p.Name,
                    Slug = p.Slug,
                    Description = p.Description,
                    Price = p.Price,
                    Currency = p.Currency,
                    Images = ParseImages(p.ImagesJson),
                    Specifications = ParseSpecifications(p.SpecificationsJson),
                    Status = p.Status,
                    IsReservable = p.IsReservable,
                    ReservationCount = p.Reservations.Count(r => r.Status != "cancelled"),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsDeleted = p.IsDeleted,
                    DeletedAt = p.DeletedAt
                }).ToList();

                return new ApiResponse<List<AdminProductDto>>(productDtos, "Products retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<AdminProductDto>>(
                    message: "Failed to retrieve products",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminProductDto>> GetProductByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Reservations)
                    .FirstOrDefaultAsync(p => p.Id == id, ct);

                if (product == null)
                {
                    return new ApiResponse<AdminProductDto>("Product not found", false);
                }

                var dto = new AdminProductDto
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
                    IsReservable = product.IsReservable,
                    ReservationCount = product.Reservations.Count(r => r.Status != "cancelled"),
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    IsDeleted = product.IsDeleted,
                    DeletedAt = product.DeletedAt
                };

                return new ApiResponse<AdminProductDto>(dto, "Product retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminProductDto>(
                    message: "Failed to retrieve product",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminProductDto>> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
        {
            try
            {
                // Check if category exists
                var categoryExists = await _context.StoreCategories
                    .AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted, ct);

                if (!categoryExists)
                {
                    return new ApiResponse<AdminProductDto>("Category not found", false);
                }

                var slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug;

                // Check for duplicate slug
                var existingSlug = await _context.StoreProducts
                    .AnyAsync(p => p.Slug == slug, ct);

                if (existingSlug)
                {
                    return new ApiResponse<AdminProductDto>("Product with this slug already exists", false);
                }

                var product = new StoreProduct
                {
                    CategoryId = dto.CategoryId,
                    Name = dto.Name,
                    Slug = slug,
                    Description = dto.Description,
                    Price = dto.Price,
                    Currency = dto.Currency,
                    ImagesJson = dto.Images != null ? JsonSerializer.Serialize(dto.Images) : null,
                    SpecificationsJson = dto.Specifications != null ? JsonSerializer.Serialize(dto.Specifications) : null,
                    Status = dto.Status,
                    IsReservable = dto.IsReservable,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StoreProducts.Add(product);
                await _context.SaveChangesAsync(ct);

                // Get category name
                var category = await _context.StoreCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == product.CategoryId, ct);

                var resultDto = new AdminProductDto
                {
                    Id = product.Id,
                    CategoryId = product.CategoryId,
                    CategoryName = category?.Name ?? "",
                    Name = product.Name,
                    Slug = product.Slug,
                    Description = product.Description,
                    Price = product.Price,
                    Currency = product.Currency,
                    Images = dto.Images ?? new List<string>(),
                    Specifications = dto.Specifications,
                    Status = product.Status,
                    IsReservable = product.IsReservable,
                    ReservationCount = 0,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    IsDeleted = product.IsDeleted,
                    DeletedAt = product.DeletedAt
                };

                return new ApiResponse<AdminProductDto>(resultDto, "Product created successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminProductDto>(
                    message: "Failed to create product",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminProductDto>> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .Include(p => p.Category)
                    .Include(p => p.Reservations)
                    .FirstOrDefaultAsync(p => p.Id == id, ct);

                if (product == null)
                {
                    return new ApiResponse<AdminProductDto>("Product not found", false);
                }

                if (dto.CategoryId.HasValue)
                {
                    var categoryExists = await _context.StoreCategories
                        .AnyAsync(c => c.Id == dto.CategoryId.Value && !c.IsDeleted, ct);

                    if (!categoryExists)
                    {
                        return new ApiResponse<AdminProductDto>("Category not found", false);
                    }
                    product.CategoryId = dto.CategoryId.Value;
                }

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    product.Name = dto.Name;

                if (!string.IsNullOrWhiteSpace(dto.Slug))
                {
                    var existingSlug = await _context.StoreProducts
                        .AnyAsync(p => p.Slug == dto.Slug && p.Id != id, ct);

                    if (existingSlug)
                    {
                        return new ApiResponse<AdminProductDto>("Product with this slug already exists", false);
                    }
                    product.Slug = dto.Slug;
                }

                if (dto.Description != null)
                    product.Description = dto.Description;

                if (dto.Price.HasValue)
                    product.Price = dto.Price.Value;

                if (!string.IsNullOrWhiteSpace(dto.Currency))
                    product.Currency = dto.Currency;

                if (dto.Images != null)
                    product.ImagesJson = JsonSerializer.Serialize(dto.Images);

                if (dto.Specifications != null)
                    product.SpecificationsJson = JsonSerializer.Serialize(dto.Specifications);

                if (!string.IsNullOrWhiteSpace(dto.Status))
                    product.Status = dto.Status;

                if (dto.IsReservable.HasValue)
                    product.IsReservable = dto.IsReservable.Value;

                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                // Reload category if changed
                if (dto.CategoryId.HasValue)
                {
                    await _context.Entry(product).Reference(p => p.Category).LoadAsync(ct);
                }

                var resultDto = new AdminProductDto
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
                    IsReservable = product.IsReservable,
                    ReservationCount = product.Reservations.Count(r => r.Status != "cancelled"),
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    IsDeleted = product.IsDeleted,
                    DeletedAt = product.DeletedAt
                };

                return new ApiResponse<AdminProductDto>(resultDto, "Product updated successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminProductDto>(
                    message: "Failed to update product",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> DeleteProductAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .FirstOrDefaultAsync(p => p.Id == id, ct);

                if (product == null)
                {
                    return new ApiResponse<bool>("Product not found", false);
                }

                product.IsDeleted = true;
                product.DeletedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return new ApiResponse<bool>(true, "Product deleted successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to delete product",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> RestoreProductAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted, ct);

                if (product == null)
                {
                    return new ApiResponse<bool>("Product not found or not deleted", false);
                }

                product.IsDeleted = false;
                product.DeletedAt = null;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return new ApiResponse<bool>(true, "Product restored successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to restore product",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> ForceDeleteProductAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .Include(p => p.Reservations)
                    .FirstOrDefaultAsync(p => p.Id == id, ct);

                if (product == null)
                {
                    return new ApiResponse<bool>("Product not found", false);
                }

                if (product.Reservations.Any(r => r.Status != "cancelled"))
                {
                    return new ApiResponse<bool>("Cannot permanently delete product with active reservations", false);
                }

                _context.StoreProducts.Remove(product);
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<bool>(true, "Product permanently deleted", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to permanently delete product",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        #endregion

        #region Reservations

        public async Task<ApiResponse<PagedResult<AdminReservationDto>>> GetReservationsAsync(
            string? status = null,
            string? paymentStatus = null,
            string? deliveryStatus = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? search = null,
            PaginationParams? pagination = null,
            CancellationToken ct = default)
        {
            try
            {
                pagination ??= new PaginationParams();

                var query = _context.StoreReservations
                    .AsNoTracking()
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(r => r.Status == status);

                if (!string.IsNullOrWhiteSpace(paymentStatus))
                    query = query.Where(r => r.PaymentStatus == paymentStatus);

                if (!string.IsNullOrWhiteSpace(deliveryStatus))
                    query = query.Where(r => r.DeliveryStatus == deliveryStatus);

                if (fromDate.HasValue)
                    query = query.Where(r => r.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(r => r.CreatedAt <= toDate.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(r =>
                        (r.User != null && r.User.FullName.Contains(search)) ||
                        (r.User != null && r.User.Email != null && r.User.Email.Contains(search)) ||
                        (r.User != null && r.User.PhoneNumber != null && r.User.PhoneNumber.Contains(search)) ||
                        (r.Product != null && r.Product.Name.Contains(search)));
                }

                var totalCount = await query.CountAsync(ct);

                var reservations = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .Select(r => new AdminReservationDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserName = r.User != null ? r.User.FullName : "",
                        UserEmail = r.User != null ? r.User.Email : null,
                        UserPhone = r.User != null ? r.User.PhoneNumber : null,
                        ProductId = r.ProductId,
                        ProductName = r.Product != null ? r.Product.Name : "",
                        ProductThumbnail = r.Product != null ? GetFirstImage(r.Product.ImagesJson) : null,
                        Quantity = r.Quantity,
                        UnitPrice = r.UnitPrice,
                        TotalPrice = r.TotalPrice,
                        Currency = r.Product != null ? r.Product.Currency : "EGP",
                        Status = r.Status,
                        PaymentStatus = r.PaymentStatus,
                        PaymentMethod = r.PaymentMethod,
                        PaymentReference = r.PaymentReference,
                        PaidAmount = r.PaidAmount,
                        PaidAt = r.PaidAt,
                        DeliveryStatus = r.DeliveryStatus,
                        DeliveredAt = r.DeliveredAt,
                        DeliveryNotes = r.DeliveryNotes,
                        AdminNotes = r.AdminNotes,
                        ContactedAt = r.ContactedAt,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync(ct);

                var result = new PagedResult<AdminReservationDto>(reservations, totalCount, pagination.PageNumber, pagination.PageSize);
                return new ApiResponse<PagedResult<AdminReservationDto>>(result, "Reservations retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<AdminReservationDto>>(
                    message: "Failed to retrieve reservations",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminReservationDto>> GetReservationByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var reservation = await _context.StoreReservations
                    .AsNoTracking()
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (reservation == null)
                {
                    return new ApiResponse<AdminReservationDto>("Reservation not found", false);
                }

                var dto = new AdminReservationDto
                {
                    Id = reservation.Id,
                    UserId = reservation.UserId,
                    UserName = reservation.User?.FullName ?? "",
                    UserEmail = reservation.User?.Email,
                    UserPhone = reservation.User?.PhoneNumber,
                    ProductId = reservation.ProductId,
                    ProductName = reservation.Product?.Name ?? "",
                    ProductThumbnail = GetFirstImage(reservation.Product?.ImagesJson),
                    Quantity = reservation.Quantity,
                    UnitPrice = reservation.UnitPrice,
                    TotalPrice = reservation.TotalPrice,
                    Currency = reservation.Product?.Currency ?? "EGP",
                    Status = reservation.Status,
                    PaymentStatus = reservation.PaymentStatus,
                    PaymentMethod = reservation.PaymentMethod,
                    PaymentReference = reservation.PaymentReference,
                    PaidAmount = reservation.PaidAmount,
                    PaidAt = reservation.PaidAt,
                    DeliveryStatus = reservation.DeliveryStatus,
                    DeliveredAt = reservation.DeliveredAt,
                    DeliveryNotes = reservation.DeliveryNotes,
                    AdminNotes = reservation.AdminNotes,
                    ContactedAt = reservation.ContactedAt,
                    CreatedAt = reservation.CreatedAt,
                    UpdatedAt = reservation.UpdatedAt
                };

                return new ApiResponse<AdminReservationDto>(dto, "Reservation retrieved successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminReservationDto>(
                    message: "Failed to retrieve reservation",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminReservationDto>> RecordContactAsync(int id, RecordContactDto dto, CancellationToken ct = default)
        {
            try
            {
                var reservation = await _context.StoreReservations
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (reservation == null)
                {
                    return new ApiResponse<AdminReservationDto>("Reservation not found", false);
                }

                reservation.Status = "contacted";
                reservation.ContactedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.AdminNotes))
                    reservation.AdminNotes = dto.AdminNotes;
                reservation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return await GetReservationByIdAsync(id, ct);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminReservationDto>(
                    message: "Failed to record contact",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminReservationDto>> RecordPaymentAsync(int id, RecordPaymentDto dto, CancellationToken ct = default)
        {
            try
            {
                var reservation = await _context.StoreReservations
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (reservation == null)
                {
                    return new ApiResponse<AdminReservationDto>("Reservation not found", false);
                }

                // TODO: Re-enable this check when online payment is implemented
                // if (reservation.Status == "pending")
                // {
                //     return new ApiResponse<AdminReservationDto>("Must contact customer before recording payment", false);
                // }

                reservation.PaymentStatus = "paid";
                reservation.PaymentMethod = string.IsNullOrEmpty(dto.PaymentMethod) ? "manual" : dto.PaymentMethod;
                reservation.PaymentReference = dto.PaymentReference;
                reservation.PaidAmount = (dto.PaidAmount == null || dto.PaidAmount <= 0) ? reservation.TotalPrice : dto.PaidAmount.Value;
                reservation.PaidAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.AdminNotes))
                    reservation.AdminNotes = dto.AdminNotes;
                reservation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return await GetReservationByIdAsync(id, ct);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminReservationDto>(
                    message: "Failed to record payment",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminReservationDto>> RecordDeliveryAsync(int id, RecordDeliveryDto dto, CancellationToken ct = default)
        {
            try
            {
                var reservation = await _context.StoreReservations
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (reservation == null)
                {
                    return new ApiResponse<AdminReservationDto>("Reservation not found", false);
                }

                if (reservation.PaymentStatus != "paid")
                {
                    return new ApiResponse<AdminReservationDto>("Must record payment before delivery", false);
                }

                reservation.DeliveryStatus = "delivered";
                reservation.DeliveredAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.DeliveryNotes))
                    reservation.DeliveryNotes = dto.DeliveryNotes;
                if (!string.IsNullOrWhiteSpace(dto.AdminNotes))
                    reservation.AdminNotes = dto.AdminNotes;
                reservation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return await GetReservationByIdAsync(id, ct);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminReservationDto>(
                    message: "Failed to record delivery",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<AdminReservationDto>> CompleteReservationAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var reservation = await _context.StoreReservations
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

                if (reservation == null)
                {
                    return new ApiResponse<AdminReservationDto>("Reservation not found", false);
                }

                if (reservation.PaymentStatus != "paid" || reservation.DeliveryStatus != "delivered")
                {
                    return new ApiResponse<AdminReservationDto>("Cannot complete reservation. Payment and delivery must be completed first.", false);
                }

                reservation.Status = "completed";
                reservation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return await GetReservationByIdAsync(id, ct);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AdminReservationDto>(
                    message: "Failed to complete reservation",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> CancelReservationAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var reservation = await _context.StoreReservations
                    .FirstOrDefaultAsync(r => r.Id == id, ct);

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

        private static string GenerateSlug(string name)
        {
            var slug = name.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');
            return slug + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

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

        #endregion
    }
}
