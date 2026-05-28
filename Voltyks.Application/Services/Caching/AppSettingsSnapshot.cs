namespace Voltyks.Application.Services.Caching
{
    public class AppSettingsSnapshot
    {
        public bool ChargingModeEnabled { get; set; }
        public bool AdminsModeActivated { get; set; }
        public DateTime? ChargingModeEnabledAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
