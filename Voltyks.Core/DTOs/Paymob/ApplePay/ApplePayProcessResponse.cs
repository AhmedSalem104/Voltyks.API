using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    /// <summary>
    /// Response DTO for Apple Pay Server-to-Server processing
    /// </summary>
    public class ApplePayProcessResponse
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public long? PaymobOrderId { get; set; }
        public string? MerchantOrderId { get; set; }
        public string Status { get; set; } = "unknown";
        public string? Message { get; set; }
        public long AmountCents { get; set; }
        public string Currency { get; set; } = "EGP";
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public bool IsPending { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
