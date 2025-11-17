namespace Voltyks.AdminControlDashboard.Dtos.Fees
{
    public class AdminFeesDto
    {
        public int Id { get; set; }
        public decimal MinimumFee { get; set; }
        public decimal Percentage { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
