using System.Collections.Generic;

namespace Voltyks.AdminControlDashboard.Dtos.Notifications
{
    public class TemplateUseDto
    {
        public string Key { get; set; } = default!;
        public Dictionary<string, string>? Params { get; set; }
    }

    public class CustomMessageDto
    {
        public string TitleEn { get; set; } = default!;
        public string? TitleAr { get; set; }
        public string BodyEn { get; set; } = default!;
        public string? BodyAr { get; set; }
    }

    public class SendToUserDto
    {
        public string UserId { get; set; } = default!;
        public string Mode { get; set; } = "template"; // "template" | "custom"
        public TemplateUseDto? Template { get; set; }
        public CustomMessageDto? Custom { get; set; }
    }

    public class BroadcastAudienceDto
    {
        public string Type { get; set; } = "all"; // "all" | "role" | "city" | "users"
        public string? Role { get; set; }          // "vehicle_owner" | "charger_owner"
        public string? City { get; set; }
        public List<string>? UserIds { get; set; }
    }

    public class BroadcastDto
    {
        public BroadcastAudienceDto Audience { get; set; } = new();
        public string Mode { get; set; } = "template";
        public TemplateUseDto? Template { get; set; }
        public CustomMessageDto? Custom { get; set; }
    }

    public class BroadcastResultDto
    {
        public int BroadcastId { get; set; }
        public int RecipientCount { get; set; }
        public int DbPersistedCount { get; set; }
        public int FcmAttemptedCount { get; set; }
        public int FcmSucceededCount { get; set; }
    }
}
