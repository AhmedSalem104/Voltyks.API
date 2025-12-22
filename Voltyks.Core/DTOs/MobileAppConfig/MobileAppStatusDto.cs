using System.Text.Json.Serialization;

namespace Voltyks.Core.DTOs.MobileAppConfig
{
    public class MobileAppStatusDto
    {
        [JsonPropertyName("mobile_app_enabled")]
        public bool MobileAppEnabled { get; set; }
    }
}
