namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    /// <summary>
    /// Response DTO for Apple Pay payment status verification
    /// </summary>
    public class ApplePayVerifyResponse
    {
        /// <summary>
        /// The merchant order ID
        /// </summary>
        public string MerchantOrderId { get; set; } = string.Empty;

        /// <summary>
        /// Paymob order ID (if available)
        /// </summary>
        public long? PaymobOrderId { get; set; }

        /// <summary>
        /// Current payment status: Pending, Paid, Failed, Voided, Refunded
        /// </summary>
        public string Status { get; set; } = "Unknown";

        /// <summary>
        /// Whether the payment has been confirmed as paid
        /// </summary>
        public bool IsPaid { get; set; }

        /// <summary>
        /// Whether the payment is still pending confirmation
        /// </summary>
        public bool IsPending { get; set; }

        /// <summary>
        /// Paymob transaction ID (if available)
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Amount in cents
        /// </summary>
        public long AmountCents { get; set; }

        /// <summary>
        /// Currency code
        /// </summary>
        public string Currency { get; set; } = "EGP";

        /// <summary>
        /// Timestamp when payment was confirmed (if paid)
        /// </summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// Failure reason (if status is Failed)
        /// </summary>
        public string? FailureReason { get; set; }
    }
}
