namespace Voltyks.AdminControlDashboard.Dtos.ComplaintCategories
{
    public class AdminComplaintCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int ComplaintsCount { get; set; }
    }
}
