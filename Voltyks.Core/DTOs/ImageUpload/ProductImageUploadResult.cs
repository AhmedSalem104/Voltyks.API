namespace Voltyks.Core.DTOs.ImageUpload
{
    public class ProductImageUploadResult
    {
        public int ProductId { get; set; }
        public List<string> UploadedUrls { get; set; } = new();
        public List<string> AllImages { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
