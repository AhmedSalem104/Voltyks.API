namespace Voltyks.Persistence.Entities.Main.Paymob
{
    public class CardTokenWebhookLog : BaseEntity<int>
    {
        /// <summary>
        /// Unique webhook identifier for idempotency
        /// </summary>
        public string WebhookId { get; set; } = default!;

        /// <summary>
        /// Resolved user ID (null if resolution failed)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Card token from Paymob
        /// </summary>
        public string? CardToken { get; set; }

        /// <summary>
        /// Last 4 digits of card
        /// </summary>
        public string? Last4 { get; set; }

        /// <summary>
        /// Card brand (visa, mastercard, etc.)
        /// </summary>
        public string? Brand { get; set; }

        /// <summary>
        /// Card expiry month
        /// </summary>
        public int? ExpiryMonth { get; set; }

        /// <summary>
        /// Card expiry year
        /// </summary>
        public int? ExpiryYear { get; set; }

        /// <summary>
        /// Processing status
        /// </summary>
        public CardTokenStatus Status { get; set; }

        /// <summary>
        /// Reason for failure (if any)
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Full webhook payload JSON
        /// </summary>
        public string RawPayload { get; set; } = default!;

        /// <summary>
        /// Whether HMAC signature was valid
        /// </summary>
        public bool IsHmacValid { get; set; }

        /// <summary>
        /// When webhook was received
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When processing completed
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// FK to saved card (if saved successfully)
        /// </summary>
        public int? SavedCardId { get; set; }

        /// <summary>
        /// Navigation property to saved card
        /// </summary>
        public UserSavedCard? SavedCard { get; set; }
    }

    public enum CardTokenStatus
    {
        Pending = 0,
        Saved = 1,
        Duplicate = 2,
        FailedNoUser = 3,
        FailedNoToken = 4,
        FailedHmac = 5,
        FailedDatabase = 6
    }
}
