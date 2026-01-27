namespace Voltyks.Core.DTOs.Process
{
    /// <summary>
    /// UI context that exactly mirrors the FCM notification data payload.
    /// Keys and values match what is sent via push notifications.
    /// </summary>
    public class PendingProcessUiContext
    {
        // Base notification fields (always present)
        public string RequestId { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;

        // Timer fields (for pending, accepted statuses)
        public string? TimerStartedAt { get; set; }
        public string? TimerDurationMinutes { get; set; }

        // Process/payment fields (for PendingCompleted, Started statuses)
        public string? ProcessId { get; set; }
        public string? EstimatedPrice { get; set; }
        public string? AmountCharged { get; set; }
        public string? AmountPaid { get; set; }
    }
}
