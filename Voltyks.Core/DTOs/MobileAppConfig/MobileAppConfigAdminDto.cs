using System.Text.Json.Serialization;

namespace Voltyks.Core.DTOs.MobileAppConfig
{
    /// <summary>
    /// Response DTO for admin - full config view
    /// </summary>
    public class MobileAppConfigAdminDto
    {
        [JsonPropertyName("android_enabled")]
        public bool AndroidEnabled { get; set; }

        [JsonPropertyName("ios_enabled")]
        public bool IosEnabled { get; set; }

        [JsonPropertyName("android_min_version")]
        public string? AndroidMinVersion { get; set; }

        [JsonPropertyName("ios_min_version")]
        public string? IosMinVersion { get; set; }
    }
}
