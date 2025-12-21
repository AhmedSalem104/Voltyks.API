namespace Voltyks.AdminControlDashboard.Dtos.Notifications
{
    public class AdminNotificationDto
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public int OriginalId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? UserName { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}
