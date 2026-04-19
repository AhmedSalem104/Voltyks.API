namespace Voltyks.Core.DTOs.VehicleAdditionRequests
{
    public class UserVehicleAdditionRequestDto
    {
        public int Id { get; set; }
        public string BrandName { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string Capacity { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
