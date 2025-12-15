namespace Voltyks.AdminControlDashboard.Dtos.Capacity
{
    public class AdminCapacityDto
    {
        public int Id { get; set; }
        public int KW { get; set; }
    }

    public class CreateCapacityDto
    {
        public int KW { get; set; }
    }

    public class UpdateCapacityDto
    {
        public int KW { get; set; }
    }
}
