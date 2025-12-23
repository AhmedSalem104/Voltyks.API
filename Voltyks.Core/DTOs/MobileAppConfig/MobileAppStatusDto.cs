using System.Text.Json.Serialization;

namespace Voltyks.Core.DTOs.MobileAppConfig
{
    /// <summary>
    /// Response for public mobile-status endpoint
    /// </summary>
    public class MobileAppStatusDto
    {
        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("is_version_valid")]
        public bool IsVersionValid { get; set; }

        [JsonPropertyName("min_version")]
        public string? MinVersion { get; set; }
    }

    /// <summary>
    /// Response for old mobile-enabled endpoint (backwards compatible)
    /// </summary>
    public class MobileAppEnabledDto
    {
        [JsonPropertyName("mobile_app_enabled")]
        public bool MobileAppEnabled { get; set; }
    }
}
