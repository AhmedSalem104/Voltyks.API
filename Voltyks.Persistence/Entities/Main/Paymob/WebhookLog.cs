using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main.Paymob
{

    public class WebhookLog :BaseEntity<int>
    {
        public string EventType { get; set; } = default!;                 
        public string? MerchantOrderId { get; set; }
        public long? PaymobOrderId { get; set; }
        public long? PaymobTransactionId { get; set; }
        public bool IsHmacValid { get; set; }
        public int? HttpStatus { get; set; }
        public string? HeadersJson { get; set; }
        public string RawPayload { get; set; } = default!;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public bool IsValid { get; set; }                              
        public long? MerchantId { get; set; }


    }
}
