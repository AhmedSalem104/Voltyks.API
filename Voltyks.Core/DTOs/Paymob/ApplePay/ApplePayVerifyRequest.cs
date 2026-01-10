using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    /// <summary>
    /// Request DTO for verifying Apple Pay payment status
    /// </summary>
    public class ApplePayVerifyRequest
    {
        /// <summary>
        /// The merchant order ID returned from the original payment request
        /// </summary>
        [Required]
        public string MerchantOrderId { get; set; } = string.Empty;
    }
}
