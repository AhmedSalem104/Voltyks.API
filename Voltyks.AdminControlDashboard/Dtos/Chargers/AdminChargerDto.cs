namespace Voltyks.AdminControlDashboard.Dtos.Chargers
{
    public class AdminChargerDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }

        public int ProtocolId { get; set; }
        public string ProtocolName { get; set; }

        public int CapacityId { get; set; }
        public int CapacityKw { get; set; }

        public int PriceOptionId { get; set; }
        public decimal PriceValue { get; set; }

        public int AddressId { get; set; }
        public string Area { get; set; }
        public string Street { get; set; }
        public string BuildingNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool? Adaptor { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
