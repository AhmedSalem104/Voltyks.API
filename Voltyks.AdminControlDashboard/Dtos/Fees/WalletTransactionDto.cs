namespace Voltyks.AdminControlDashboard.Dtos.Fees
{
    public class WalletTransactionDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = default!;  // "Add" or "Deduct"
        public string? Notes { get; set; }
        public double PreviousBalance { get; set; }
        public double NewBalance { get; set; }
        public string? CreatedByAdminId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
