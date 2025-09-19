using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Core_API_DTOs
{
    public class PaymobOrderReq
    {
        //public string auth_token { get; set; }
        //public long amount_cents { get; set; }

        //public string currency { get; set; }

        //public string merchant_order_id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("auth_token")]
        public string auth_token { get; set; } = default!;

        [System.Text.Json.Serialization.JsonPropertyName("delivery_needed")]
        public bool delivery_needed { get; set; } = false;

        [System.Text.Json.Serialization.JsonPropertyName("amount_cents")]
        public long amount_cents { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("currency")]
        public string currency { get; set; } = "EGP";

        [System.Text.Json.Serialization.JsonPropertyName("merchant_order_id")]
        public string merchant_order_id { get; set; } = default!;

        [System.Text.Json.Serialization.JsonPropertyName("items")]
        public object[] items { get; set; } = Array.Empty<object>();

    }
       
 
}
