namespace Voltyks.Persistence.Entities.Main
{
    public class AppSettings : BaseEntity<int>
    {
        public bool ChargingModeEnabled { get; set; } = false;
        public DateTime? ChargingModeEnabledAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
