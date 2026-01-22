namespace Voltyks.Core.DTOs.Store.Products
{
    public class StoreProductListDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "EGP";
        public string? ThumbnailImage { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsReservable { get; set; }
        public bool IsReserved { get; set; }
    }
}
