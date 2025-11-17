namespace Voltyks.AdminControlDashboard.Dtos.Reports
{
    public class AdminReportFilterDto
    {
        public string? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsResolved { get; set; }
    }
}
