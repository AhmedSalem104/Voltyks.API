using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public class OrderStatusDto
    {
        public OrderStatusDto(
            string merchantOrderId,
            string orderStatus,
            string txStatus,
            bool isSuccess,
            long amountCents,
            string currency,
            long? paymobOrderId,
            long? paymobTransactionId,
            DateTime lastUpdated)
        {
            MerchantOrderId = merchantOrderId;
            OrderStatus = orderStatus;
            TransactionStatus = txStatus;
            IsSuccess = isSuccess;
            AmountCents = amountCents;
            Currency = currency;
            PaymobOrderId = paymobOrderId;
            PaymobTransactionId = paymobTransactionId;
            LastUpdated = lastUpdated;
        }

        public string MerchantOrderId { get; set; }
        public string OrderStatus { get; set; }
        public string TransactionStatus { get; set; }
        public bool IsSuccess { get; set; }
        public long AmountCents { get; set; }
        public string Currency { get; set; }
        public long? PaymobOrderId { get; set; }
        public long? PaymobTransactionId { get; set; }
        public DateTime LastUpdated { get; set; }
    }

}
