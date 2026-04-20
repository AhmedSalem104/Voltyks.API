using Voltyks.Persistence.Utilities;

namespace Voltyks.Persistence.Entities.Main
{
    public class ComplaintCategory : BaseEntity<int>
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTimeHelper.GetEgyptTime();
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation
        public virtual ICollection<UserGeneralComplaint> Complaints { get; set; } = new List<UserGeneralComplaint>();
    }
}
