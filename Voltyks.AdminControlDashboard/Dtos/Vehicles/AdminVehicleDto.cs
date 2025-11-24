namespace Voltyks.AdminControlDashboard.Dtos.Vehicles
{
    public class AdminVehicleDto
    {
        public int Id { get; set; }
        public string Color { get; set; }
        public string Plate { get; set; }
        public DateTime CreationDate { get; set; }
        public int Year { get; set; }
        public bool IsDeleted { get; set; }

        public int BrandId { get; set; }
        public string BrandName { get; set; }

        public int ModelId { get; set; }
        public string ModelName { get; set; }
        public double ModelCapacity { get; set; }

        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }
    }
}
