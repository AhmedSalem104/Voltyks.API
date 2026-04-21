namespace Voltyks.Core.DTOs.MobileAppConfig
{
    /// <summary>
    /// Combined app-config response. Rolls up the fields the mobile app
    /// needs on launch (platform kill-switch + version gate + backend flags)
    /// into a single round-trip, replacing the three separate calls to
    /// mobile-status, charging-mode-status, and registration-status.
    /// </summary>
    public class AppInfoDto
    {
        // --- from mobile-status ---
        public bool IsEnabled { get; set; }
        public bool IsVersionValid { get; set; }
        public string? MinVersion { get; set; }

        // --- from charging-mode-status ---
        public bool ChargingModeEnabled { get; set; }

        // --- from registration-status ---
        public bool AdminsModeActivated { get; set; }
    }
}
