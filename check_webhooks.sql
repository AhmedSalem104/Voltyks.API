-- Check recent webhooks
SELECT TOP 20 
    Id,
    EventType,
    MerchantOrderId,
    IsHmacValid,
    IsValid,
    ReceivedAt
FROM WebhookLogs 
ORDER BY ReceivedAt DESC;

-- Check card token webhook status
SELECT TOP 20
    Id,
    WebhookId,
    UserId,
    LEFT(CardToken, 20) as TokenPreview,
    Last4,
    Brand,
    Status,
    FailureReason,
    ReceivedAt
FROM CardTokenWebhookLogs
ORDER BY ReceivedAt DESC;

-- Check saved cards
SELECT TOP 10
    Id,
    UserId,
    Last4,
    Brand,
    CreatedAt
FROM UserSavedCards
ORDER BY CreatedAt DESC;
