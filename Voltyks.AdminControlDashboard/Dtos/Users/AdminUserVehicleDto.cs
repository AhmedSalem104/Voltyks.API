namespace Voltyks.AdminControlDashboard.Dtos.Users
{
    public class AdminUserVehicleDto
    {
        public int Id { get; set; }
        public string Color { get; set; }
        public string Plate { get; set; }
        public int Year { get; set; }
        public DateTime CreationDate { get; set; }
        public string BrandName { get; set; }
        public string ModelName { get; set; }
        public bool IsDeleted { get; set; }
    }
}
