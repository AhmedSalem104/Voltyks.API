namespace Voltyks.Core.DTOs.Store.Categories
{
    public class StoreCategoryListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? PlaceholderMessage { get; set; }
        public int ProductCount { get; set; }
    }
}
