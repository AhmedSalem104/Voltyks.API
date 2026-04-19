namespace Voltyks.AdminControlDashboard.Dtos.VehicleAdditionRequests
{
    public class AdminVehicleAdditionRequestDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string UserFullName { get; set; } = "";
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public string BrandName { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string Capacity { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ProcessedBy { get; set; }
    }
}
