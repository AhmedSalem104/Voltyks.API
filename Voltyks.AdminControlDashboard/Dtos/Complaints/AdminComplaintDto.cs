namespace Voltyks.AdminControlDashboard.Dtos.Complaints
{
    public class AdminComplaintDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
    }
}
