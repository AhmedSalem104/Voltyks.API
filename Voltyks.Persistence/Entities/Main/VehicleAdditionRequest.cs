using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class VehicleAdditionRequest : BaseEntity<int>
    {
        public string UserId { get; set; } = default!;
        public string BrandName { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public string Capacity { get; set; } = default!;
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? ProcessedBy { get; set; }

        public AppUser? User { get; set; }
    }
}
