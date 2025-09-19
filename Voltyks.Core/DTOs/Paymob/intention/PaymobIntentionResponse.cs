using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.intention
{

    public sealed class PaymobIntentionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = "";

        [JsonPropertyName("intention_order_id")]
        public int IntentionOrderId { get; set; }

        [JsonPropertyName("redirection_url")]
        public string? RedirectionUrl { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        // ده Array of objects راجع من Paymob
        [JsonPropertyName("payment_keys")]
        public List<PaymobPaymentKey>? PaymentKeys { get; set; }
    }

    public sealed class PaymobPaymentKey
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = "";

        [JsonPropertyName("integration")]
        public int Integration { get; set; }

        [JsonPropertyName("gateway_type")]
        public string? GatewayType { get; set; }

        [JsonPropertyName("redirection_url")]
        public string? RedirectionUrl { get; set; }
    }
}
