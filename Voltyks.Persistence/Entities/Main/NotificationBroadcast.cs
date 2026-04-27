using Voltyks.Persistence.Utilities;

namespace Voltyks.Persistence.Entities.Main
{
    /// <summary>
    /// Audit row for an admin-issued broadcast. Stores who fanned out, the audience
    /// filter, and aggregate delivery counts so abuse / failures can be diagnosed
    /// long after the fact. One row per /admin/notifications/broadcast call.
    /// </summary>
    public class NotificationBroadcast : BaseEntity<int>
    {
        public string AdminUserId { get; set; } = default!;

        public string AudienceJson { get; set; } = "{}";

        public int RecipientCount { get; set; }
        public int DbPersistedCount { get; set; }
        public int FcmAttemptedCount { get; set; }
        public int FcmSucceededCount { get; set; }

        public string Title { get; set; } = default!;
        public string Body { get; set; } = default!;

        /// <summary>Template key used (null when mode=custom).</summary>
        public string? TemplateKey { get; set; }

        public DateTime SentAt { get; set; } = DateTimeHelper.GetEgyptTime();
    }
}
