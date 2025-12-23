using System.Text.Json.Serialization;
using Voltyks.Persistence.Entities.Main.Paymob;

namespace Voltyks.Core.DTOs.Paymob.CardsDTOs
{
    /// <summary>
    /// DTO for viewing card token webhook logs
    /// </summary>
    public class CardTokenWebhookLogDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("webhook_id")]
        public string WebhookId { get; set; } = default!;

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("card_token")]
        public string? CardToken { get; set; }

        [JsonPropertyName("last4")]
        public string? Last4 { get; set; }

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("expiry_month")]
        public int? ExpiryMonth { get; set; }

        [JsonPropertyName("expiry_year")]
        public int? ExpiryYear { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = default!;

        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("failure_reason")]
        public string? FailureReason { get; set; }

        [JsonPropertyName("is_hmac_valid")]
        public bool IsHmacValid { get; set; }

        [JsonPropertyName("received_at")]
        public DateTime ReceivedAt { get; set; }

        [JsonPropertyName("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        [JsonPropertyName("saved_card_id")]
        public int? SavedCardId { get; set; }
    }

    /// <summary>
    /// DTO for detailed webhook log view (includes raw payload)
    /// </summary>
    public class CardTokenWebhookLogDetailDto : CardTokenWebhookLogDto
    {
        [JsonPropertyName("raw_payload")]
        public string RawPayload { get; set; } = default!;
    }

    /// <summary>
    /// Summary statistics for webhook logs
    /// </summary>
    public class CardTokenWebhookStatsDto
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("saved")]
        public int Saved { get; set; }

        [JsonPropertyName("duplicate")]
        public int Duplicate { get; set; }

        [JsonPropertyName("failed_no_user")]
        public int FailedNoUser { get; set; }

        [JsonPropertyName("failed_no_token")]
        public int FailedNoToken { get; set; }

        [JsonPropertyName("failed_hmac")]
        public int FailedHmac { get; set; }

        [JsonPropertyName("failed_database")]
        public int FailedDatabase { get; set; }

        [JsonPropertyName("top_failure_reasons")]
        public List<FailureReasonCount> TopFailureReasons { get; set; } = new();
    }

    public class FailureReasonCount
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = default!;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
