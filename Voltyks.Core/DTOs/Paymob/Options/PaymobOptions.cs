using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Options
{
    public class PaymobOptions
    {
        public string ApiBase { get; set; } = default!;
        public string ApiKey { get; set; } = default!;
        public string HmacSecret { get; set; } = default!;
        public string SecretKey { get; set; } = default!;
        public string? WebhookTestKey { get; set; }  // API key for /webhook/test endpoint (Dev/Staging only)

        public string IframeId { get; set; } = default!;
        public string Currency { get; set; } = "EGP";
        public string ENV { get; set; } = "EGP";

        public IntegrationIds Integration { get; set; } = new();



        // 👇 جديد
        public string? PublicKey { get; set; }
        public IntentionOptions Intention { get; set; } = new();

        public class IntegrationOptions
        {
            public int Card { get; set; }
            public int Wallet { get; set; }
        }
        public class IntentionOptions
        {
            public string? Url { get; set; }   // URL كامل (اختياري)
            public string? Path { get; set; }  // Path يُركّب على ApiBase لو Url فاضي
        }


    }
}
