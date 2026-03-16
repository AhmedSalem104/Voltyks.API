-- Check if table exists and count logs
SELECT 
    (SELECT COUNT(*) FROM CardTokenWebhookLogs) as TotalLogs,
    (SELECT COUNT(*) FROM CardTokenWebhookLogs WHERE Status = 0) as Pending,
    (SELECT COUNT(*) FROM CardTokenWebhookLogs WHERE Status = 1) as Saved,
    (SELECT COUNT(*) FROM CardTokenWebhookLogs WHERE Status = 2) as Duplicate,
    (SELECT COUNT(*) FROM CardTokenWebhookLogs WHERE Status = 3) as FailedNoUser,
    (SELECT COUNT(*) FROM CardTokenWebhookLogs WHERE Status = 4) as FailedNoToken,
    (SELECT COUNT(*) FROM CardTokenWebhookLogs WHERE Status = 5) as FailedHmac,
    (SELECT COUNT(*) FROM CardTokenWebhookLogs WHERE Status = 6) as FailedDatabase;
