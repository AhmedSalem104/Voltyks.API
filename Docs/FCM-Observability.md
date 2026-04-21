# FCM Failure Observability

This doc explains how to observe FCM push-notification failures in Voltyks.API and how to swap the telemetry backend without touching business logic.

## What gets recorded

Every FCM call flows through `FirebaseService.SendNotificationAsync`. That single method:

1. On HTTP success → calls `IFcmTelemetry.RecordSent(notificationType)`.
2. On FCM error response (any 4xx/5xx) → calls `IFcmTelemetry.RecordFailed(notificationType, errorCode)` and emits a structured `LogWarning`.
3. On transport/credential/timeout exception → calls `IFcmTelemetry.RecordFailed(notificationType, exceptionClass)` and emits `LogError`.

Additionally, every **call site** emits one `FCM batch done` log line after the DB write + FCM attempt, with `UserId`, `NotificationType`, `TokenCount`, `PersistedToDb`.

**The DB `Notifications` row is always written regardless of FCM outcome.** Telemetry only describes the push side channel.

## Log templates

### Per-failure (inside `FirebaseService`)

```
FCM send failed. TokenSuffix={TokenSuffix} NotificationType={NotificationType}
RequestId={RequestId} StatusCode={StatusCode} ErrorCode={ErrorCode}
```

`TokenSuffix` is the last 8 characters of the token — enough to correlate with DB rows without logging full PII.

`ErrorCode` values surfaced by `ExtractFcmErrorCode`:

| Code | Meaning | Typical action |
|---|---|---|
| `UNREGISTERED` | App was uninstalled or FCM token expired | Token should be deleted from `DeviceTokens` |
| `INVALID_ARGUMENT` | Payload or token malformed | Check payload shape; delete token if persistent |
| `SENDER_ID_MISMATCH` | Token registered to different Firebase project | Force re-register |
| `QUOTA_EXCEEDED` | Rate limit | Slow down sends, raise quota |
| `UNAVAILABLE` | FCM 503 | Transient — retry later |
| `INTERNAL` | FCM 500 | Transient — retry later |
| `THIRD_PARTY_AUTH_ERROR` | APNs/web push cert problem | Rotate cert |
| `UNKNOWN` | Unrecognised | Inspect raw exception |

### Per-transport (network/credential exception)

```
FCM send exception. TokenSuffix={TokenSuffix} NotificationType={NotificationType}
RequestId={RequestId} ErrorClass={ErrorClass}
```

### Batch summary (every call site)

```
FCM batch done. UserId={UserId} NotificationType={NotificationType}
TokenCount={TokenCount} PersistedToDb={PersistedToDb}
```

## Diagnostic endpoint

```
GET /diagnostics/fcm-stats
Authorization: Bearer <admin-token>
```

Returns a `FcmTelemetrySnapshot`:

```json
{
  "sent": 142,
  "failed": 9,
  "failedByErrorCode": { "UNREGISTERED": 6, "INVALID_ARGUMENT": 2, "UNAVAILABLE": 1 },
  "failedByNotificationType": { "VehicleOwner_RequestCharger": 4, "Process_Terminated": 5 }
}
```

Counts are in-process only and reset on app-pool recycle. For persistent metrics swap the backend (see below).

## Swapping the telemetry backend

The telemetry layer is behind `IFcmTelemetry` in `Voltyks.Application.Interfaces.Telemetry`. The default registration lives in `ApplicationServicesRegisteration.cs`:

```csharp
services.AddSingleton<IFcmTelemetry, InMemoryFcmTelemetry>();
```

To swap:

### Application Insights

1. Add the `Microsoft.ApplicationInsights.AspNetCore` package.
2. Add a new implementation `ApplicationInsightsFcmTelemetry` in `Voltyks.Application/Services/Telemetry/`.
3. Replace the one DI line:
   ```csharp
   services.AddSingleton<IFcmTelemetry, ApplicationInsightsFcmTelemetry>();
   ```

No call site changes.

### Prometheus

1. Add `prometheus-net.AspNetCore`.
2. Add `PrometheusFcmTelemetry` exposing `Counter` instances for sent/failed.
3. Swap the DI line.

### OpenTelemetry

1. Add `OpenTelemetry.Extensions.Hosting` + exporter of choice.
2. Add `OtelFcmTelemetry` using a `Meter` and `Counter<long>`.
3. Swap the DI line.

Any of the above gives you persistent, dashboard-ready metrics without touching `FirebaseService` or the 7 call sites.

## What this layer does NOT do

- Does **not** retry failed sends.
- Does **not** delete invalid tokens from the `DeviceTokens` table. Only the existing Redis-cache cleanup for `UNREGISTERED` remains.
- Does **not** rate-limit the FCM outbound.
- Does **not** fire alerts or page on-call.

These are all logic changes and were intentionally left out of the telemetry layer per the project's "telemetry only" rule. Add them in separate, auditable PRs if the dashboard shows the need.

## Known issue — FCM downtime + `Task.WhenAll`

`ProcessesService.SendAndPersistNotificationAsync` and `ChargingRequestService.SendAndPersistNotificationAsync` use `Task.WhenAll` to push to all tokens in parallel. If FCM is fully down and every task throws, the method re-throws and the caller's code path aborts. **The DB row for that notification is written first, so history is preserved**, but the endpoint the user hit will return 500. This is a logic concern, not a telemetry one, and is deliberately untouched.

## Manual operations

### Find users with many dead tokens

```sql
-- Replace @UserId with a userId that's appearing with UNREGISTERED in logs
SELECT t.Id, t.Token, t.DateCreated
FROM DeviceTokens t
WHERE t.UserId = @UserId
ORDER BY t.DateCreated DESC;
```

### Delete a specific stale token

```sql
DELETE FROM DeviceTokens WHERE Id = @tokenId;
```

Do this **only** after confirming the token is stale — either by cross-referencing the `UNREGISTERED` log entries, or by having the app team confirm the device hasn't been active.
