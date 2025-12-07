namespace Voltyks.Core.DTOs.Process
{
    public class MyActivityDto
    {
        public int Id { get; set; }
        public int ChargerRequestId { get; set; }
        public string? Status { get; set; }
        public decimal? AmountCharged { get; set; }
        public decimal? AmountPaid { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateCompleted { get; set; }
        public bool IsAsChargerOwner { get; set; }
        public bool IsAsVehicleOwner { get; set; }
        public string? Direction { get; set; }
        public string? CounterpartyUserId { get; set; }
        public int MyRoleUserTypeId { get; set; }
        public double? VehicleOwnerRating { get; set; }
        public double? ChargerOwnerRating { get; set; }
        public string? ChargerProtocolName { get; set; }
        public int? ChargerCapacityKw { get; set; }
    }
}
