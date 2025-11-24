namespace Voltyks.AdminControlDashboard.Dtos.Vehicles
{
    public class AdminCreateVehicleDto
    {
        public string Color { get; set; } = "";
        public string Plate { get; set; } = "";
        public int Year { get; set; }
        public int BrandId { get; set; }
        public int ModelId { get; set; }
        public string UserId { get; set; } = "";
    }
}
