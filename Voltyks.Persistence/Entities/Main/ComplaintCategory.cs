namespace Voltyks.Persistence.Entities.Main
{
    public class ComplaintCategory : BaseEntity<int>
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation
        public virtual ICollection<UserGeneralComplaint> Complaints { get; set; } = new List<UserGeneralComplaint>();
    }
}
