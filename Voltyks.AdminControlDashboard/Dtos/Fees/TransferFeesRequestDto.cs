namespace Voltyks.AdminControlDashboard.Dtos.Fees
{
    public class TransferFeesRequestDto
    {
        public string RecipientUserId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }
}
