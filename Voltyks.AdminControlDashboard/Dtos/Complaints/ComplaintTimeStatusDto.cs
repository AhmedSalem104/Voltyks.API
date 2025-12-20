namespace Voltyks.AdminControlDashboard.Dtos.Complaints
{
    public class ComplaintTimeStatusDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOver12Hours { get; set; }
        public double HoursElapsed { get; set; }
    }
}
