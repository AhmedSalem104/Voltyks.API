using System;
using System.Collections.Generic;

namespace Voltyks.AdminControlDashboard.Dtos.Notifications
{
    /// <summary>
    /// One row in the admin "notification templates" UI. Combines the
    /// hardcoded registry metadata with the (optional) DB override.
    /// </summary>
    public class NotificationTemplateDto
    {
        public string Key { get; set; } = default!;
        public string TitleEn { get; set; } = default!;
        public string TitleAr { get; set; } = default!;
        public string BodyEn { get; set; } = default!;
        public string BodyAr { get; set; } = default!;
        public List<string> RequiredParams { get; set; } = new();
        public bool IsCustomized { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class UpdateNotificationTemplateDto
    {
        public string TitleEn { get; set; } = default!;
        public string TitleAr { get; set; } = default!;
        public string BodyEn { get; set; } = default!;
        public string BodyAr { get; set; } = default!;
    }

    public class TemplatePreviewRequestDto
    {
        public string Lang { get; set; } = "en";
        public Dictionary<string, string>? Params { get; set; }
    }

    public class TemplatePreviewResultDto
    {
        public string Title { get; set; } = default!;
        public string Body { get; set; } = default!;
        public bool FromDb { get; set; }
    }
}
