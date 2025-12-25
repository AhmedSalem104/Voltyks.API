using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.ImageUpload;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.ImageUpload;
using Voltyks.Persistence.Data;

namespace Voltyks.Application.Services.ImageUpload
{
    public class ProductImageService : IProductImageService
    {
        private readonly VoltyksDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly Dictionary<string, byte[]> MagicBytes = new()
        {
            { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
            { ".webp", new byte[] { 0x52, 0x49, 0x46, 0x46 } }
        };

        public ProductImageService(VoltyksDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<ApiResponse<ProductImageUploadResult>> UploadImagesAsync(
            int productId,
            List<IFormFile> files,
            CancellationToken ct = default)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return new ApiResponse<ProductImageUploadResult>("No images provided", false);
                }

                // Get product
                var product = await _context.StoreProducts
                    .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

                if (product == null)
                {
                    return new ApiResponse<ProductImageUploadResult>("Product not found", false);
                }

                // Create folder path
                var folderRelativePath = $"images/products/{productId}_{product.Slug}";
                var fullFolderPath = Path.Combine(_environment.WebRootPath, folderRelativePath);
                Directory.CreateDirectory(fullFolderPath);

                var uploadedUrls = new List<string>();
                var errors = new List<string>();

                foreach (var file in files)
                {
                    var validationError = ValidateFile(file);
                    if (validationError != null)
                    {
                        errors.Add($"{file.FileName}: {validationError}");
                        continue;
                    }

                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{extension}";
                    var filePath = Path.Combine(fullFolderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream, ct);
                    }

                    uploadedUrls.Add($"/{folderRelativePath}/{fileName}");
                }

                if (uploadedUrls.Count == 0 && errors.Count > 0)
                {
                    return new ApiResponse<ProductImageUploadResult>(
                        message: "All uploads failed",
                        status: false,
                        errors: errors);
                }

                // Update product ImagesJson (append new URLs)
                var existingImages = ParseImages(product.ImagesJson);
                existingImages.AddRange(uploadedUrls);
                product.ImagesJson = JsonSerializer.Serialize(existingImages);
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                var result = new ProductImageUploadResult
                {
                    ProductId = productId,
                    UploadedUrls = uploadedUrls,
                    AllImages = existingImages,
                    TotalCount = existingImages.Count
                };

                if (errors.Count > 0)
                {
                    return new ApiResponse<ProductImageUploadResult>(
                        result,
                        "Some images uploaded with errors",
                        true,
                        errors);
                }

                return new ApiResponse<ProductImageUploadResult>(
                    result,
                    "Images uploaded successfully",
                    true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ProductImageUploadResult>(
                    message: "Failed to upload images",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> DeleteImageAsync(
            int productId,
            string imagePath,
            CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

                if (product == null)
                {
                    return new ApiResponse<bool>("Product not found", false);
                }

                var existingImages = ParseImages(product.ImagesJson);

                if (!existingImages.Contains(imagePath))
                {
                    return new ApiResponse<bool>("Image not found in product", false);
                }

                // Remove from list
                existingImages.Remove(imagePath);
                product.ImagesJson = JsonSerializer.Serialize(existingImages);
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                // Delete file if it's a local file
                if (imagePath.StartsWith("/images/products/"))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                return new ApiResponse<bool>(true, "Image deleted successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to delete image",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> DeleteAllProductImagesAsync(
            int productId,
            CancellationToken ct = default)
        {
            try
            {
                var product = await _context.StoreProducts
                    .FirstOrDefaultAsync(p => p.Id == productId, ct);

                if (product == null)
                {
                    return new ApiResponse<bool>("Product not found", false);
                }

                // Delete folder if exists
                var folderRelativePath = $"images/products/{productId}_{product.Slug}";
                var fullFolderPath = Path.Combine(_environment.WebRootPath, folderRelativePath);

                if (Directory.Exists(fullFolderPath))
                {
                    Directory.Delete(fullFolderPath, recursive: true);
                }

                // Clear images JSON (remove only local images, keep external URLs if needed)
                var existingImages = ParseImages(product.ImagesJson);
                var externalImages = existingImages.Where(img => !img.StartsWith("/images/products/")).ToList();

                product.ImagesJson = externalImages.Count > 0
                    ? JsonSerializer.Serialize(externalImages)
                    : null;
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                return new ApiResponse<bool>(true, "All local images deleted successfully", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(
                    message: "Failed to delete images",
                    status: false,
                    errors: new List<string> { ex.Message });
            }
        }

        private string? ValidateFile(IFormFile file)
        {
            if (file.Length == 0)
                return "File is empty";

            if (file.Length > MaxFileSize)
                return $"File size {file.Length / (1024.0 * 1024.0):F1}MB exceeds 5MB limit";

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return "Invalid file type. Only jpg, png, webp allowed";

            // Validate magic bytes
            using var stream = file.OpenReadStream();
            var buffer = new byte[8];
            stream.Read(buffer, 0, 8);

            var expectedMagic = MagicBytes.GetValueOrDefault(extension);
            if (expectedMagic != null && !buffer.Take(expectedMagic.Length).SequenceEqual(expectedMagic))
                return "File content does not match extension";

            return null;
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
    }
}
