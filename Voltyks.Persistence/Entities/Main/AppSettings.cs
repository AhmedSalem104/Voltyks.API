namespace Voltyks.Persistence.Entities.Main
{
    public class AppSettings : BaseEntity<int>
    {
        public bool ChargingModeEnabled { get; set; } = false;
        public DateTime? ChargingModeEnabledAt { get; set; }
        public bool AdminsModeActivated { get; set; } = false;
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
