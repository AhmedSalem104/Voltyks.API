namespace Voltyks.AdminControlDashboard.Dtos.Users
{
    public class CreateAdminDto
    {
        public string OtpCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
