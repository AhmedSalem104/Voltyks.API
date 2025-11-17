namespace Voltyks.AdminControlDashboard.Dtos.Users
{
    public class AdminUserDetailsDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? NationalId { get; set; }
        public bool IsBanned { get; set; }
        public bool IsAvailable { get; set; }
        public double Rating { get; set; }
        public int RatingCount { get; set; }
        public double? Wallet { get; set; }
        public DateTime DateCreated { get; set; }
        public int TotalChargers { get; set; }
        public int TotalVehicles { get; set; }
        public int TotalChargingRequests { get; set; }
    }
}
