namespace Voltyks.AdminControlDashboard.Dtos.Reports
{
    public class AdminReportDetailsDto
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }
        public DateTime ReportDate { get; set; }
        public string ReportContent { get; set; }
        public bool IsResolved { get; set; }
    }
}
