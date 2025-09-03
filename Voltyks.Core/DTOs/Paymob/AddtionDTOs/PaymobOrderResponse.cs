using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public class PaymobOrderResponse
    {
        public int Id { get; set; }
        public string MerchantOrderId { get; set; }
        public int AmountCents { get; set; }
        public string Currency { get; set; }
        public bool DeliveryNeeded { get; set; }
        public string Status { get; set; }
        public List<PaymobTransactionResponse> PaymentDetails { get; set; }
    }

}
