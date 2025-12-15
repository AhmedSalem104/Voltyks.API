# Voltyks API - Payment System Issues & Fixes

> **Reference Document** - Issues identified in the Payment system that need fixing

---

## Table of Contents
1. [Summary](#summary)
2. [Critical Security Issues](#critical-security-issues)
3. [High Priority Issues](#high-priority-issues)
4. [Medium Priority Issues](#medium-priority-issues)
5. [Implementation Plan](#implementation-plan)
6. [Code Examples](#code-examples)

---

## Summary

| Severity | Count | Status |
|----------|-------|--------|
| **CRITICAL** | 2 | Pending |
| **HIGH** | 4 | Pending |
| **MEDIUM** | 5 | Pending |
| **Total** | 11 | Pending |

### Files to Modify
- `Voltyks.Application/Services/Paymob/PaymobService.cs`
- `Voltyks.API/Controllers/PaymentController.cs`

---

## Critical Security Issues

### Issue #1: HMAC Webhook Signature Verification DISABLED

**File:** `PaymobService.cs`
**Lines:** 833-841
**Severity:** 🔴 CRITICAL

**Current Code:**
```csharp
private bool VerifyWebhookSignature(HttpRequest req, string rawBody)
{
    // Example sketch:
    // var sigHeader = req.Headers["X-Signature"].FirstOrDefault();
    // var tsHeader  = req.Headers["X-Timestamp"].FirstOrDefault();
    // var secret    = _options.WebhookSecret;
    // var expected  = HmacSha256($"{tsHeader}.{rawBody}", secret);
    // return TimeSafeEquals(sigHeader, expected) && TsWithinSkew(tsHeader);
    return true;  // ⚠️ ALWAYS RETURNS TRUE - NO VERIFICATION!
}
```

**Problem:**
- Any external attacker can send fake webhook requests
- Payment status can be spoofed/manipulated
- Fake card tokens can be saved to database
- Potential financial fraud

**Fix Required:**
```csharp
private bool VerifyWebhookSignature(HttpRequest req, string rawBody)
{
    // Get HMAC from request (Paymob sends it in query string or header)
    var hmacFromPaymob = req.Query["hmac"].FirstOrDefault()
        ?? req.Headers["X-Hmac"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(hmacFromPaymob))
    {
        _log?.LogWarning("Webhook received without HMAC signature");
        return false;
    }

    // Paymob HMAC calculation - concatenate specific fields in order
    // Reference: https://docs.paymob.com/docs/transaction-webhooks
    var fieldsToHash = new[]
    {
        "amount_cents", "created_at", "currency", "error_occured",
        "has_parent_transaction", "id", "integration_id", "is_3d_secure",
        "is_auth", "is_capture", "is_refunded", "is_standalone_payment",
        "is_voided", "order.id", "owner", "pending", "source_data.pan",
        "source_data.sub_type", "source_data.type", "success"
    };

    // Extract values from webhook payload and concatenate
    var concatenated = ExtractAndConcatenateFields(rawBody, fieldsToHash);

    // Calculate HMAC-SHA512
    using var hmac = new System.Security.Cryptography.HMACSHA512(
        Encoding.UTF8.GetBytes(_opt.HmacSecret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
    var calculatedHmac = BitConverter.ToString(hash).Replace("-", "").ToLower();

    // Time-safe comparison to prevent timing attacks
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(hmacFromPaymob.ToLower()),
        Encoding.UTF8.GetBytes(calculatedHmac));
}
```

---

### Issue #2: Console.WriteLine Debug Statements in Production

**File:** `PaymobService.cs`
**Lines:** 1047, 1127, 1133
**Severity:** 🔴 CRITICAL

**Current Code:**
```csharp
// Line 1047
Console.WriteLine($"HMAC Signature: {hmacSignature}");

// Line 1127
Console.WriteLine("Body String: " + bodyString);

// Line 1133
Console.WriteLine("HMAC Signature: " + hmacSignature);
```

**Problem:**
- Sensitive HMAC signatures exposed in server logs
- Security vulnerability - attackers with log access can forge requests
- Performance impact in production
- Unprofessional code

**Fix Required:**
```csharp
// Remove all Console.WriteLine statements
// OR replace with proper logging at Debug level (disabled in production):

_log?.LogDebug("HMAC calculation completed for order {OrderId}", orderId);
// NEVER log the actual signature or sensitive data!
```

---

## High Priority Issues

### Issue #3: Null Reference in JSON Parsing

**File:** `PaymobService.cs`
**Lines:** 1070-1072, 1112
**Severity:** 🟠 HIGH

**Current Code:**
```csharp
// Line 1070-1072
using var doc = JsonDocument.Parse(raw);
var root = doc.RootElement;
var clientSecret = root.GetProperty("client_secret").GetString();  // ⚠️ Throws if missing!

// Line 1112
return root.GetProperty("token").GetString();  // ⚠️ Throws if missing!
```

**Problem:**
- `GetProperty()` throws `KeyNotFoundException` if property doesn't exist
- Application crashes on unexpected API responses
- No graceful error handling

**Fix Required:**
```csharp
// Line 1070-1072 - Safe extraction
using var doc = JsonDocument.Parse(raw);
var root = doc.RootElement;

if (!root.TryGetProperty("client_secret", out var clientSecretProp) ||
    clientSecretProp.ValueKind != JsonValueKind.String)
{
    _log?.LogError("Missing or invalid client_secret in response");
    return new ApiResponse<IntentionClientSecretDto>(
        null, "Invalid API response - missing client_secret", false);
}
var clientSecret = clientSecretProp.GetString();

// Line 1112 - Safe extraction
if (!root.TryGetProperty("token", out var tokenProp) ||
    tokenProp.ValueKind != JsonValueKind.String)
{
    _log?.LogError("Missing or invalid token in auth response");
    throw new InvalidOperationException("Failed to get auth token from Paymob");
}
return tokenProp.GetString();
```

---

### Issue #4: HTTP Response Not Validated

**File:** `PaymobService.cs`
**Lines:** 1611-1614, 1388
**Severity:** 🟠 HIGH

**Current Code:**
```csharp
// Line 1611-1614
var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/payment_keys", body);
var data = await res.Content.ReadFromJsonAsync<PaymobPaymentKeyRes>();
var key = data!.token;  // ⚠️ No check if request succeeded!

// Line 1388
var res = await HttpPostJsonWithRetryAsync(url, body);
// ⚠️ Immediately reads response without checking success status
```

**Problem:**
- Response deserialized even on 4xx/5xx errors
- Null reference exception on failed requests
- Error details lost

**Fix Required:**
```csharp
// Line 1611-1614
var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/payment_keys", body);

if (!res.IsSuccessStatusCode)
{
    var errorContent = await res.Content.ReadAsStringAsync();
    _log?.LogError("Payment key creation failed: {Status} - {Body}",
        res.StatusCode, errorContent);
    return new ApiResponse<SavedCardPaymentResponse>(
        null, $"Failed to create payment key: {res.StatusCode}", false);
}

var data = await res.Content.ReadFromJsonAsync<PaymobPaymentKeyRes>();
if (data?.token == null)
{
    _log?.LogError("Payment key response missing token");
    return new ApiResponse<SavedCardPaymentResponse>(
        null, "Invalid payment key response", false);
}
var key = data.token;
```

---

### Issue #5: Token Not Validated After Fetch

**File:** `PaymobService.cs`
**Lines:** 72-73, 109, 159
**Severity:** 🟠 HIGH

**Current Code:**
```csharp
// Lines 72-73
var _ = await _tokenProvider.GetAsync();  // مجرد ضمان، الاستخدام الفعلي بيتم داخل الخطوات التالية
// Token fetched but result ignored and not validated!
```

**Problem:**
- Token might be null or empty
- Subsequent operations fail with cryptic errors
- No retry on token failure

**Fix Required:**
```csharp
// Fetch and validate token
var authToken = await _tokenProvider.GetAsync();
if (string.IsNullOrWhiteSpace(authToken))
{
    // Invalidate cache and retry once
    await _tokenProvider.InvalidateAsync();
    authToken = await _tokenProvider.GetAsync();

    if (string.IsNullOrWhiteSpace(authToken))
    {
        _log?.LogError("Failed to obtain Paymob auth token after retry");
        return new ApiResponse<CardCheckoutResponse>(
            null, "Authentication failed with payment provider", false);
    }
}
```

---

### Issue #6: Unsafe Null-Forgiving Operator on Payment Key

**File:** `PaymobService.cs`
**Line:** 1614
**Severity:** 🟠 HIGH

**Current Code:**
```csharp
var key = data!.token;  // ⚠️ Using ! assumes data is never null
```

**Problem:**
- If `data` is null, throws `NullReferenceException`
- Compiler warning suppressed but bug not fixed
- Runtime crash possible

**Fix Required:**
```csharp
if (data?.token == null)
{
    _log?.LogError("Payment key response was null or missing token");
    return new ApiResponse<SavedCardPaymentResponse>(
        null, "Failed to generate payment key", false);
}
var key = data.token;
```

---

## Medium Priority Issues

### Issue #7: Card Token Logged in Plain Text (PCI-DSS Violation)

**File:** `PaymobService.cs`
**Lines:** 813-814
**Severity:** 🟡 MEDIUM

**Current Code:**
```csharp
_log?.LogWarning("Saved NEW card → user={userId}, last4={last4}, brand={brand}, exp={m}/{y}, token={token}",
    userId, last4, brand, expMonth, expYear, cardToken);  // ⚠️ Full token logged!
```

**Problem:**
- Full card token written to application logs
- PCI-DSS compliance violation
- Security risk if logs are compromised

**Fix Required:**
```csharp
// Log only necessary info, hash or truncate sensitive data
_log?.LogWarning("Saved NEW card → user={userId}, last4={last4}, brand={brand}, exp={m}/{y}",
    userId, last4, brand, expMonth, expYear);
// Token should NEVER be logged, even partially
```

---

### Issue #8: Empty Catch Block Swallows Errors

**File:** `PaymobService.cs`
**Line:** 671
**Severity:** 🟡 MEDIUM

**Current Code:**
```csharp
catch
{
    // ignore parsing errors  ⚠️ Silent failure!
}
```

**Problem:**
- All exceptions silently swallowed
- No logging - impossible to debug issues
- Webhook processed with incomplete/corrupt data

**Fix Required:**
```csharp
catch (JsonException ex)
{
    _log?.LogWarning(ex, "Failed to parse webhook JSON payload. Processing with available data.");
    // Continue with partial data if possible
}
catch (Exception ex)
{
    _log?.LogError(ex, "Unexpected error parsing webhook payload");
    // Decide: return error or continue with partial data
}
```

---

### Issue #9: Retry Loop Potential Integer Overflow

**File:** `PaymobService.cs`
**Line:** 1531
**Severity:** 🟡 MEDIUM

**Current Code:**
```csharp
for (int attempt = 0; ; attempt++)  // ⚠️ Infinite loop if condition never met!
{
    var res = await _http.PostAsJsonAsync(url, body, ct);
    if (res.StatusCode != (HttpStatusCode)429) return res;

    if (attempt >= maxRetries) return res;
    // ...
}
```

**Problem:**
- `attempt++` will eventually overflow (int.MaxValue + 1)
- Theoretical infinite loop risk
- Code structure unclear

**Fix Required:**
```csharp
int attempt = 0;
while (attempt < maxRetries)
{
    var res = await _http.PostAsJsonAsync(url, body, ct);

    if (res.StatusCode != (HttpStatusCode)429)
        return res;  // Success or non-retryable error

    attempt++;
    if (attempt >= maxRetries)
    {
        _log?.LogWarning("Max retries ({MaxRetries}) reached for {Url}", maxRetries, url);
        return res;
    }

    // Exponential backoff
    var delaySeconds = Math.Min(Math.Pow(2, attempt), 8);
    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
}
```

---

### Issue #10: Zero Amount Accepted as Default

**File:** `PaymobService.cs`
**Line:** 202
**Severity:** 🟡 MEDIUM

**Current Code:**
```csharp
long amountCents = root.TryGetProperty("amount_cents", out var ac)
    && ac.TryGetInt64(out var a)
    ? a
    : 0;  // ⚠️ 0 means "free" - dangerous default!
```

**Problem:**
- If parsing fails, amount defaults to 0
- Could process payments as free
- Financial impact

**Fix Required:**
```csharp
long amountCents;
if (!root.TryGetProperty("amount_cents", out var ac) || !ac.TryGetInt64(out amountCents))
{
    _log?.LogError("Missing or invalid amount_cents in order response");
    return new ApiResponse<OrderStatusDto>(
        null, "Invalid order response - missing amount", false);
}

if (amountCents <= 0)
{
    _log?.LogWarning("Order has zero or negative amount: {Amount}", amountCents);
    // Decide based on business rules whether to proceed
}
```

---

### Issue #11: No Request Size Limit on Webhook Endpoint

**File:** `PaymentController.cs`
**Lines:** 93-104
**Severity:** 🟡 MEDIUM

**Current Code:**
```csharp
[HttpPost("webhook")]
[AllowAnonymous]
public async Task<IActionResult> Webhook()
{
    Request.EnableBuffering();
    using var reader = new StreamReader(Request.Body);
    var raw = await reader.ReadToEndAsync();  // ⚠️ No size limit!
    // ...
}
```

**Problem:**
- Attacker can send huge payload to exhaust memory
- Denial of Service (DoS) vulnerability
- No protection against malformed requests

**Fix Required:**
```csharp
[HttpPost("webhook")]
[AllowAnonymous]
[RequestSizeLimit(1_048_576)]  // 1 MB limit
[RequestFormLimits(MultipartBodyLengthLimit = 1_048_576)]
public async Task<IActionResult> Webhook()
{
    Request.EnableBuffering();

    // Additional check for body length
    if (Request.ContentLength > 1_048_576)
    {
        _log?.LogWarning("Webhook payload too large: {Size} bytes", Request.ContentLength);
        return BadRequest("Payload too large");
    }

    using var reader = new StreamReader(Request.Body);
    var raw = await reader.ReadToEndAsync();
    Request.Body.Position = 0;

    var res = await _svc.HandleWebhookAsync(Request, raw);
    return Ok(res);
}
```

---

## Implementation Plan

### Phase 1: Critical Security (Do First!)
| Step | Issue | Estimated Effort |
|------|-------|------------------|
| 1 | Implement HMAC verification | 2-3 hours |
| 2 | Remove Console.WriteLine | 15 minutes |

### Phase 2: Null Safety
| Step | Issue | Estimated Effort |
|------|-------|------------------|
| 3 | Safe JSON extraction | 1 hour |
| 4 | HTTP response validation | 1 hour |
| 5 | Token validation | 30 minutes |
| 6 | Payment key null check | 15 minutes |

### Phase 3: Robustness
| Step | Issue | Estimated Effort |
|------|-------|------------------|
| 7 | Remove token from logs | 15 minutes |
| 8 | Add exception logging | 30 minutes |
| 9 | Fix retry loop | 30 minutes |
| 10 | Zero amount validation | 30 minutes |
| 11 | Add request size limit | 15 minutes |

**Total Estimated Time:** 6-8 hours

---

## Code Examples

### Helper Method: Safe JSON Property Extraction

```csharp
private static T? SafeGetJsonProperty<T>(JsonElement root, string propertyName,
    Func<JsonElement, T> extractor, T? defaultValue = default)
{
    if (!root.TryGetProperty(propertyName, out var prop))
        return defaultValue;

    try
    {
        return extractor(prop);
    }
    catch
    {
        return defaultValue;
    }
}

// Usage:
var clientSecret = SafeGetJsonProperty(root, "client_secret", p => p.GetString(), null);
var amountCents = SafeGetJsonProperty(root, "amount_cents", p => p.GetInt64(), 0L);
```

### Helper Method: HTTP Response Validation

```csharp
private async Task<ApiResponse<T>> ValidateAndDeserializeAsync<T>(
    HttpResponseMessage response, string operationName) where T : class
{
    if (!response.IsSuccessStatusCode)
    {
        var errorBody = await response.Content.ReadAsStringAsync();
        _log?.LogError("{Operation} failed with {Status}: {Body}",
            operationName, response.StatusCode, errorBody);

        return new ApiResponse<T>(null, $"{operationName} failed: {response.StatusCode}", false,
            new List<string> { errorBody });
    }

    try
    {
        var data = await response.Content.ReadFromJsonAsync<T>();
        if (data == null)
        {
            _log?.LogError("{Operation} returned null response", operationName);
            return new ApiResponse<T>(null, "Empty response from payment provider", false);
        }
        return new ApiResponse<T>(data, "Success", true);
    }
    catch (JsonException ex)
    {
        _log?.LogError(ex, "{Operation} response deserialization failed", operationName);
        return new ApiResponse<T>(null, "Invalid response format", false);
    }
}
```

---

## Testing Checklist

After implementing fixes, verify:

- [ ] HMAC verification rejects invalid signatures
- [ ] HMAC verification accepts valid Paymob webhooks
- [ ] No Console.WriteLine in production logs
- [ ] JSON parsing doesn't crash on missing fields
- [ ] HTTP errors are properly logged and handled
- [ ] Token failures trigger retry logic
- [ ] Card tokens not present in any logs
- [ ] Large webhook payloads are rejected
- [ ] Zero amount orders are handled per business rules

---

## References

- [Paymob Webhook Documentation](https://docs.paymob.com/docs/transaction-webhooks)
- [Paymob HMAC Verification](https://docs.paymob.com/docs/hmac-calculation)
- [PCI-DSS Logging Requirements](https://www.pcisecuritystandards.org/)

---

*Document Created: December 9, 2025*
*Status: Pending Implementation*
