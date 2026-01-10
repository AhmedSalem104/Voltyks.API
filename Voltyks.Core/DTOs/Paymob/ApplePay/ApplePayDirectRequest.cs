using System.ComponentModel.DataAnnotations;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    /// <summary>
    /// Request DTO for Server-to-Server Apple Pay processing.
    /// Mobile app sends the Apple Pay token (Base64) from PKPayment.token.paymentData
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
        /// Apple Pay token (Base64 encoded) from iOS PKPayment.token.paymentData
        /// </summary>
        [Required(ErrorMessage = "Apple Pay token is required")]
        public string ApplePayToken { get; set; } = string.Empty;

        /// <summary>
        /// Billing data for the transaction
        /// </summary>
        [Required]
        public BillingDataInitDto BillingData { get; set; } = null!;

        /// <summary>
        /// Optional currency (defaults to EGP)
        /// </summary>
        public string Currency { get; set; } = "EGP";
    }
}
