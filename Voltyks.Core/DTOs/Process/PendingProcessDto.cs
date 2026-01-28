namespace Voltyks.Core.DTOs.Process
{
    public class PendingProcessDto
    {
        // Identifiers
        public int? ProcessId { get; set; }
        public int RequestId { get; set; }

        // Status
        public string Status { get; set; } = string.Empty;
        public string SubStatus { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        // UI Context - exactly mirrors FCM notification data payload
        public PendingProcessUiContext UiContext { get; set; } = new();

        // Resume context for frontend navigation
        public ResumeContext Resume { get; set; } = new();

        // Timestamps
        public DateTime CreatedAt { get; set; }
    }
}
