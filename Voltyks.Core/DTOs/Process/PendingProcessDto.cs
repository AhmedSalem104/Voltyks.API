namespace Voltyks.Core.DTOs.Process
{
    public class PendingProcessDto
    {
        // Identifiers
        public int? ProcessId { get; set; }
        public int RequestId { get; set; }

        // Type and status
        public string Type { get; set; } = "charging_request";
        public string Status { get; set; } = string.Empty;
        public string SubStatus { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        // UI Context - exactly mirrors FCM notification data payload
        public PendingProcessUiContext UiContext { get; set; } = new();

        // Resume context for frontend navigation
        public ResumeContext Resume { get; set; } = new();

        // Additional UI rendering data
        public PendingProcessRenderData RenderData { get; set; } = new();

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Additional data for UI rendering (not part of notification payload).
    /// </summary>
    public class PendingProcessRenderData
    {
        // Counterparty info
        public string CounterpartyUserId { get; set; } = string.Empty;
        public string CounterpartyName { get; set; } = string.Empty;
        public string? CounterpartyPhone { get; set; }

        // Charger details
        public int ChargerId { get; set; }
        public string? ChargerProtocolName { get; set; }
        public int? ChargerCapacityKw { get; set; }
        public double? KwNeeded { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Financial summary (typed, for display)
        public decimal? EstimatedPrice { get; set; }
        public decimal? BaseAmount { get; set; }
        public decimal? VoltyksFees { get; set; }
        public decimal? AmountCharged { get; set; }
        public decimal? AmountPaid { get; set; }

        // Available actions for the user
        public List<string> AvailableActions { get; set; } = new();
    }
}
