namespace Voltyks.AdminControlDashboard.Dtos.Terms
{
    public class AdminTermsDto
    {
        public int Version { get; set; }
        public string Lang { get; set; }
        public DateTime PublishedAt { get; set; }
        public object Content { get; set; }
    }
}
