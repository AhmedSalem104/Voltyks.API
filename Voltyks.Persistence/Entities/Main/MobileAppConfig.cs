namespace Voltyks.Persistence.Entities.Main
{
    public class MobileAppConfig : BaseEntity<int>
    {
        // Kill-switch per platform
        public bool AndroidEnabled { get; set; } = true;
        public bool IosEnabled { get; set; } = true;

        // Minimum required versions (semver format: "1.2.0")
        public string? AndroidMinVersion { get; set; }
        public string? IosMinVersion { get; set; }
    }
}
