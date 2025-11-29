using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class UserGeneralComplaint : BaseEntity<int>
    {
        public string UserId { get; set; } = default!;
        public int CategoryId { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;

        // Navigation
        public virtual AppUser? User { get; set; }
        public virtual ComplaintCategory? Category { get; set; }
    }
}
