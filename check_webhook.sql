SELECT TOP 5 
    Id,
    WebhookId,
    Status,
    FailureReason,
    UserId,
    CardToken,
    Last4,
    Brand,
    IsHmacValid,
    ReceivedAt,
    ProcessedAt
FROM CardTokenWebhookLogs
ORDER BY ReceivedAt DESC
