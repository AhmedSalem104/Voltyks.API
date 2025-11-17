namespace Voltyks.AdminControlDashboard.Dtos.Users
{
    public class AdminUserDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsBanned { get; set; }
        public bool IsAvailable { get; set; }
        public double Rating { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
