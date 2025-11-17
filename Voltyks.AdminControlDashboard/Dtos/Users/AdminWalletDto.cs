namespace Voltyks.AdminControlDashboard.Dtos.Users
{
    public class AdminWalletDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public double? WalletBalance { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
