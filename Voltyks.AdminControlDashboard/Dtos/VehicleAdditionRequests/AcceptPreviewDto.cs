namespace Voltyks.AdminControlDashboard.Dtos.VehicleAdditionRequests
{
    public class AcceptPreviewDto
    {
        public OriginalSubmissionDto Original { get; set; } = new();
        public double? ParsedCapacity { get; set; }
        public bool CapacityParseSuccess { get; set; }

        public BrandSuggestionDto? ExactBrandMatch { get; set; }
        public List<BrandSuggestionDto> SimilarBrands { get; set; } = new();

        public ModelSuggestionDto? ExactModelMatch { get; set; }
        public List<ModelSuggestionDto> SimilarModels { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }

    public class OriginalSubmissionDto
    {
        public string BrandName { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string Capacity { get; set; } = "";
    }

    public class BrandSuggestionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Similarity { get; set; }
        public int ModelsCount { get; set; }
    }

    public class ModelSuggestionDto
    {
        public int ModelId { get; set; }
        public string ModelName { get; set; } = "";
        public int BrandId { get; set; }
        public string BrandName { get; set; } = "";
        public double Similarity { get; set; }
    }
}
