using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main.Paymob
{
    public class PaymentTransaction : BaseEntity<int>
    {
        public string MerchantOrderId { get; set; } = default!;           // FK → PaymentOrder
        public long? PaymobOrderId { get; set; }
        public long? PaymobTransactionId { get; set; }
        public string IntegrationType { get; set; } = "Card";             // Card/Wallet/...
        public long AmountCents { get; set; }
        public string Currency { get; set; } = "EGP";
        public string Status { get; set; } = "Initiated";                 // Paid/Failed/...
        public bool IsSuccess { get; set; }
        public long CapturedAmountCents { get; set; }
        public long RefundedAmountCents { get; set; }
        public string? GatewayResponseCode { get; set; }
        public string? GatewayResponseMessage { get; set; }
        public string? PaymentMethodMasked { get; set; }                  // لا تخزن PAN/CVV
        public string? CardBrand { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public PaymentOrder Order { get; set; } = default!;
    }
}
