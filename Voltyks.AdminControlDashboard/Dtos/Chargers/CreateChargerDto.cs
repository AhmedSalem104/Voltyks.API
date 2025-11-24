namespace Voltyks.AdminControlDashboard.Dtos.Chargers
{
    public class AdminCreateChargerDto
    {
        public string UserId { get; set; } = "";
        public int ProtocolId { get; set; }
        public int CapacityId { get; set; }
        public int PriceOptionId { get; set; }

        // Address
        public string Area { get; set; } = "";
        public string Street { get; set; } = "";
        public string BuildingNumber { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public bool IsActive { get; set; } = true;
        public bool? Adaptor { get; set; }
    }
}
