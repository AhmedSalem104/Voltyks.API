using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main.Paymob
{
    public class PaymentOrder : BaseEntity<int>
    {
        public string MerchantOrderId { get; set; } = default!;
        public long? PaymobOrderId { get; set; }
        public long AmountCents { get; set; }
        public string Currency { get; set; } = "EGP";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    }
}
