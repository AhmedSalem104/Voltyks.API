using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class WalletTransaction : BaseEntity<int>
    {
        public string UserId { get; set; } = default!;
        public AppUser User { get; set; } = default!;

        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = default!;  // "Add" or "Deduct"
        public string? Notes { get; set; }

        public double PreviousBalance { get; set; }
        public double NewBalance { get; set; }

        public string? CreatedByAdminId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
