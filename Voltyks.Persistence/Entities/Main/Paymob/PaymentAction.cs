using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main.Paymob
{
    public class PaymentAction:BaseEntity<int> // Refund/Void/Capture
    {
        public long? PaymobTransactionId { get; set; }
        public string ActionType { get; set; } = default!;                // Refund/Void/Capture
        public long RequestedAmountCents { get; set; }
        public long? ProcessedAmountCents { get; set; }
        public string Status { get; set; } = "Requested";
        public string? GatewayResponseCode { get; set; }
        public string? GatewayResponseMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
