using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    /// <summary>
    /// Request DTO for Server-to-Server Apple Pay processing.
    /// Mobile app sends the Apple Pay token from PKPayment.token.paymentData
    /// Accepts both JSON string and JSON object formats.
    /// </summary>
    public class ApplePayDirectRequest
    {
        /// <summary>
        /// Amount in cents (e.g., 10000 = 100 EGP)
        /// </summary>
        [Required]
        [Range(100, int.MaxValue, ErrorMessage = "Amount must be at least 100 cents (1 EGP)")]
        public int AmountCents { get; set; }

        /// <summary>
        /// Apple Pay token from iOS PKPayment.token.paymentData.
        /// Accepts both formats:
        /// - JSON string: "{\"data\":\"...\",\"signature\":\"...\",\"header\":{...},\"version\":\"EC_v1\"}"
        /// - JSON object: {"data":"...","signature":"...","header":{...},"version":"EC_v1"}
        /// Required fields: data, signature, header (with publicKeyHash, ephemeralPublicKey, transactionId), version
        /// </summary>
        [Required(ErrorMessage = "Apple Pay token is required")]
        public JsonElement ApplePayToken { get; set; }

        /// <summary>
        /// Billing data for the transaction
        /// </summary>
        [Required]
        public BillingDataInitDto BillingData { get; set; } = null!;

        /// <summary>
        /// Optional currency (defaults to EGP)
        /// </summary>
        public string Currency { get; set; } = "EGP";

        /// <summary>
        /// Idempotency key to prevent duplicate charges (recommended: use unique transaction ID from client).
        /// If not provided, server generates from MerchantOrderId.
        /// </summary>
        public string? IdempotencyKey { get; set; }
    }
}
