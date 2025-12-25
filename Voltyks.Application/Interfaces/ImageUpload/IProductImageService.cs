using Microsoft.AspNetCore.Http;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.ImageUpload;

namespace Voltyks.Application.Interfaces.ImageUpload
{
    public interface IProductImageService
    {
        Task<ApiResponse<ProductImageUploadResult>> UploadImagesAsync(
            int productId,
            List<IFormFile> files,
            CancellationToken ct = default);

        Task<ApiResponse<bool>> DeleteImageAsync(
            int productId,
            string imagePath,
            CancellationToken ct = default);

        Task<ApiResponse<bool>> DeleteAllProductImagesAsync(
            int productId,
            CancellationToken ct = default);
    }
}
