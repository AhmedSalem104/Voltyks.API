namespace Voltyks.AdminControlDashboard.Dtos.Process
{
    public class AdminProcessDto
    {
        // Process Info
        public int Id { get; set; }
        public int ChargerRequestId { get; set; }
        public string Status { get; set; } = "";
        public DateTime DateCreated { get; set; }
        public DateTime? DateCompleted { get; set; }

        // Payment Info
        public decimal? EstimatedPrice { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? AmountCharged { get; set; }

        // Ratings
        public double? VehicleOwnerRating { get; set; }
        public double? ChargerOwnerRating { get; set; }

        // Vehicle Owner (Car Owner) Info
        public string VehicleOwnerId { get; set; } = "";
        public string VehicleOwnerName { get; set; } = "";
        public string VehicleOwnerEmail { get; set; } = "";
        public string VehicleOwnerPhone { get; set; } = "";
        public double VehicleOwnerWallet { get; set; }
        public double VehicleOwnerUserRating { get; set; }
        public bool VehicleOwnerIsBanned { get; set; }

        // Charger Owner Info
        public string ChargerOwnerId { get; set; } = "";
        public string ChargerOwnerName { get; set; } = "";
        public string ChargerOwnerEmail { get; set; } = "";
        public string ChargerOwnerPhone { get; set; } = "";
        public double ChargerOwnerWallet { get; set; }
        public double ChargerOwnerUserRating { get; set; }
        public bool ChargerOwnerIsBanned { get; set; }

        // Charging Request Details
        public string RequestStatus { get; set; } = "";
        public double KwNeeded { get; set; }
        public int CurrentBatteryPercentage { get; set; }
        public double RequestLatitude { get; set; }
        public double RequestLongitude { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal VoltyksFees { get; set; }
        public decimal RequestEstimatedPrice { get; set; }

        // Charger Details
        public int ChargerId { get; set; }
        public string ChargerProtocol { get; set; } = "";
        public int ChargerCapacityKw { get; set; }
        public decimal ChargerPrice { get; set; }
        public bool ChargerHasAdaptor { get; set; }
        public double ChargerRating { get; set; }
        public bool ChargerIsActive { get; set; }

        // Charger Address
        public string ChargerArea { get; set; } = "";
        public string ChargerStreet { get; set; } = "";
        public string ChargerBuildingNumber { get; set; } = "";
        public double ChargerLatitude { get; set; }
        public double ChargerLongitude { get; set; }

        // Vehicle Details
        public string VehicleBrand { get; set; } = "";
        public string VehicleModel { get; set; } = "";
        public string VehicleColor { get; set; } = "";
        public string VehiclePlate { get; set; } = "";
        public int VehicleYear { get; set; }
        public double VehicleCapacity { get; set; }
    }
}
