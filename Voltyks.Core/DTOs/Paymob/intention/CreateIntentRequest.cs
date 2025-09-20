using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.intention
{
    public class CreateIntentRequest
    {
        public int Amount { get; set; }
        public string? Currency { get; set; }
        public BillingDataDto BillingData { get; set; }
        public List<ItemDto>? Items { get; set; }
        public bool EnableWallet { get; set; } = true;   // لو true هنضيف Wallet ID
        public bool EnableCard { get; set; } = true;     // لو true هنضيف Card ID
        public string? MerchantOrderId { get; set; }
        public string? NotificationUrl { get; set; }
        public string? RedirectionUrl { get; set; }
        public string? PaymentMethod { get; init; }  // "Card" or "Wallet"
                                                     // NEW
        public bool SaveCard { get; set; } = false;   // tokenize=true



        // Constructor to initialize the class
        public CreateIntentRequest(
            int amount,
            string currency,
            BillingDataDto billingData,
            List<ItemDto>? items = null,
            string? merchantOrderId = null,
            string? notificationUrl = null,
            string? redirectionUrl = null,
            string? paymentMethod = null,
             bool saveCard = false)
        {
            Amount = amount;
            Currency = currency;
            BillingData = billingData;
            Items = items;
            MerchantOrderId = merchantOrderId;
            NotificationUrl = notificationUrl;
            RedirectionUrl = redirectionUrl;
            PaymentMethod = paymentMethod;
            SaveCard = saveCard;

        }
    }

}
