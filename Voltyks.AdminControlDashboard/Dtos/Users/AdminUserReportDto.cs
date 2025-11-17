namespace Voltyks.AdminControlDashboard.Dtos.Users
{
    public class AdminUserReportDto
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public DateTime ReportDate { get; set; }
        public string ReportContent { get; set; }
        public bool IsResolved { get; set; }
    }
}
