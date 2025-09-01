using System;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces.Paymob;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;
using Voltyks.Core.DTOs.Paymob.Generic_Result_DTOs;
using Voltyks.Core.DTOs.Paymob.Input_DTOs;
using Voltyks.Core.DTOs.Paymob.Options;
using Voltyks.Persistence.Entities.Main.Paymob;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Paymob.AddtionDTOs;
using System.Net;
using System.Collections.Concurrent;

namespace Voltyks.Application.Services.Paymob
{

    public class PaymobService : IPaymobService
    {
        private readonly HttpClient _http;
        private readonly PaymobOptions _opt;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PaymobService> _log;
        private readonly IPaymobAuthTokenProvider _tokenProvider;

        private IGenericRepository<PaymentOrder, int> OrdersRepo => _uow.GetRepository<PaymentOrder, int>();
        private IGenericRepository<PaymentTransaction, int> TxRepo => _uow.GetRepository<PaymentTransaction, int>();
        private IGenericRepository<WebhookLog, int> WebhookRepo => _uow.GetRepository<WebhookLog, int>();
        private IGenericRepository<PaymentAction, int> ActionRepo => _uow.GetRepository<PaymentAction, int>();



        public PaymobService(HttpClient http, IOptions<PaymobOptions> opt, IUnitOfWork uow, ILogger<PaymobService> log, IPaymobAuthTokenProvider tokenProvider)
        {
            _http = http;
            _opt = opt.Value;
            _uow = uow;
            _log = log;
            _tokenProvider = tokenProvider;
        }

        // ===== Auth / Order / Key =====
        public async Task<ApiResponse<string>> GetAuthTokenAsync()
        {
            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/auth/tokens", new PaymobAuthReq(_opt.ApiKey));
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<PaymobAuthRes>();
            return new ApiResponse<string> { Status = true, Message = "Token issued", Data = data!.token };
        }

        public async Task<ApiResponse<int>> CreateOrderAsync(CreateOrderDto dto)
        {
            var auth = string.IsNullOrWhiteSpace(dto.AuthToken) ? await _tokenProvider.GetAsync() : dto.AuthToken;
            var currency = dto.Currency ?? _opt.Currency;

            await UpsertOrderAsync(dto.MerchantOrderId, dto.AmountCents, currency);

            var body = new PaymobOrderReq(auth_token: auth, amount_cents: dto.AmountCents, currency: currency, merchant_order_id: dto.MerchantOrderId);
            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/ecommerce/orders", body);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<PaymobOrderRes>();
            var paymobOrderId = data!.id;

            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == dto.MerchantOrderId, trackChanges: true);
            if (order != null)
            {
                order.PaymobOrderId = paymobOrderId;
                order.Status = "OrderCreated";
                order.UpdatedAt = DateTime.UtcNow;
                OrdersRepo.Update(order);
                await _uow.SaveChangesAsync();
            }

            return new ApiResponse<int> { Status = true, Message = "Order created", Data = paymobOrderId };
        }

        public async Task<ApiResponse<string>> CreatePaymentKeyAsync(CreatePaymentKeyDto dto)
        {
            var auth = string.IsNullOrWhiteSpace(dto.AuthToken) ? await _tokenProvider.GetAsync() : dto.AuthToken;
            var currency = dto.Currency ?? _opt.Currency;

            var body = new PaymobPaymentKeyReq(auth_token: auth, amount_cents: dto.AmountCents, expiration: dto.ExpirationSeconds,
                                               order_id: dto.OrderId, billing_data: dto.Billing, currency: currency,
                                               integration_id: dto.IntegrationId);

            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/payment_keys", body);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<PaymobPaymentKeyRes>();
            var paymentKey = data!.token;

            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.PaymobOrderId == dto.OrderId, trackChanges: true);
            if (order != null)
            {
                order.Status = "PaymentPending";
                order.UpdatedAt = DateTime.UtcNow;
                OrdersRepo.Update(order);
                await _uow.SaveChangesAsync();

                await AddTransactionAsync(
                    merchantOrderId: order.MerchantOrderId,
                    paymobOrderId: dto.OrderId,
                    amountCents: dto.AmountCents,
                    currency: currency,
                    integrationType: dto.IntegrationId == _opt.Integration.Card ? "Card" : "Wallet",
                    status: "Pending",
                    isSuccess: false
                );
            }

            return new ApiResponse<string> { Status = true, Message = "Payment key created", Data = paymentKey };
        }

        // ===== Service Order + Methods (Steps 1-2) =====
        public async Task<ApiResponse<string>> CreateServiceOrderAsync(CreateServiceOrderDto dto)
        {
            var merchantOrderId = EnsureMerchantOrderId(null);
            var order = await UpsertOrderAsync(merchantOrderId, dto.AmountCents, ResolveCurrency(dto.Currency));
            order.Status = "PendingPayment";
            await _uow.SaveChangesAsync();
            return new ApiResponse<string> { Status = true, Message = "Service order created", Data = merchantOrderId };
        }

        public ApiResponse<PaymentMethodsDto> GetAvailableMethods(string merchantOrderId)
        {
            var dto = new PaymentMethodsDto(
                Card: true,
                ApplePay: _opt.Integration.ApplePay > 0,
                MobileWallet: _opt.Integration.Wallet > 0,
                WalletOnly: true,
                Cash: false
            );
            return new ApiResponse<PaymentMethodsDto> { Status = true, Message = "Available methods", Data = dto };
        }

        // ===== Checkout (Steps 3-4) =====
        public async Task<ApiResponse<CardCheckoutResponse>> CheckoutCardAsync(CardCheckoutRequest req)
        {
            var currency = ResolveCurrency(req.Currency);
            var merchantOrderId = EnsureMerchantOrderId(req.MerchantOrderId);

            using (await AcquireLockAsync(merchantOrderId))
            {
                // 1) احصل/أنشئ Order ID مرة واحدة
                var paymobOrderId = await GetOrCreatePaymobOrderIdAsync(merchantOrderId, req.AmountCents, currency);

                // 2) احصل/أنشئ PaymentKey (مع كاش داخلي)
                var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);
                if (order is null)
                    return new ApiResponse<CardCheckoutResponse> { Status = false, Message = "Order not found" };

                var paymentKey = await GetOrCreatePaymentKeyAsync(order, req.AmountCents, currency, req.Billing, _opt.Integration.Card, 3600);

                // 3) Iframe URL
                var iframeUrl = BuildCardIframeUrl(paymentKey).Data!;
                var payload = new CardCheckoutResponse(merchantOrderId, paymobOrderId, paymentKey, iframeUrl);

                // 4) سجل Transaction Pending لو أول مرة
                var hasTx = (await TxRepo.GetAllAsync(t => t.MerchantOrderId == merchantOrderId, false)).Any();
                if (!hasTx)
                    await AddTransactionAsync(merchantOrderId, paymobOrderId, req.AmountCents, currency, "Card", "Pending", false);

                return new ApiResponse<CardCheckoutResponse> { Status = true, Message = "Card checkout ready", Data = payload };
            }
        }

        public async Task<ApiResponse<WalletCheckoutResponse>> CheckoutWalletAsync(WalletCheckoutRequest req)
        {
            var currency = ResolveCurrency(req.Currency);
            var merchantOrderId = EnsureMerchantOrderId(req.MerchantOrderId);

            using (await AcquireLockAsync(merchantOrderId))
            {
                var paymobOrderId = await GetOrCreatePaymobOrderIdAsync(merchantOrderId, req.AmountCents, currency);

                var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);
                if (order is null)
                    return new ApiResponse<WalletCheckoutResponse> { Status = false, Message = "Order not found" };

                var billing = new BillingData("NA", "NA", "na@example.com", req.WalletPhone, "Cairo", "EG");
                var paymentKey = await GetOrCreatePaymentKeyAsync(order, req.AmountCents, currency, billing, _opt.Integration.Wallet, 3600);

                var payRes = await PayWithWalletAsync(new WalletPaymentDto(paymentKey, req.WalletPhone));

                string? redirectUrl = null, reference = null;
                if (payRes.Data?.data is JsonElement el && el.ValueKind == JsonValueKind.Object)
                {
                    if (el.TryGetProperty("redirect_url", out var red) && red.ValueKind == JsonValueKind.String) redirectUrl = red.GetString();
                    if (el.TryGetProperty("reference", out var r) && r.ValueKind == JsonValueKind.String) reference = r.GetString();
                }

                var payload = new WalletCheckoutResponse(merchantOrderId, paymobOrderId, payRes.Status, payRes.Data?.data, redirectUrl, reference);

                var hasTx = (await TxRepo.GetAllAsync(t => t.MerchantOrderId == merchantOrderId, false)).Any();
                if (!hasTx)
                    await AddTransactionAsync(merchantOrderId, paymobOrderId, req.AmountCents, currency, "Wallet", "Pending", false);

                return new ApiResponse<WalletCheckoutResponse>
                {
                    Status = payRes.Status,
                    Message = payRes.Message,
                    Data = payload,
                    Errors = payRes.Errors
                };
            }
        }

        public async Task<ApiResponse<WalletOnlyResponse>> CheckoutWalletOnlyAsync(WalletOnlyRequest req)
        {
            var order = (await OrdersRepo.GetAllAsync(o => o.MerchantOrderId == req.MerchantOrderId, trackChanges: true)).FirstOrDefault();
            if (order is null) return new ApiResponse<WalletOnlyResponse> { Status = false, Message = "Order not found" };

            var ok = await ReserveAndCommitInternalWalletAsync(req.UserId, order.AmountCents, order.MerchantOrderId);
            if (!ok) return new ApiResponse<WalletOnlyResponse> { Status = false, Message = "Insufficient internal wallet" };

            order.Status = "Paid";
            order.UpdatedAt = DateTime.UtcNow;
            OrdersRepo.Update(order);
            await _uow.SaveChangesAsync();

            return new ApiResponse<WalletOnlyResponse> { Status = true, Message = "Paid via internal wallet", Data = new WalletOnlyResponse(order.MerchantOrderId, true, order.AmountCents, order.Currency) };
        }

        public ApiResponse<string> BuildCardIframeUrl(string paymentKey)
        {
            var url = $"https://accept.paymob.com/api/acceptance/iframes/{_opt.IframeId}?payment_token={paymentKey}";
            return new ApiResponse<string> { Status = true, Message = "iFrame URL ready", Data = url };
        }

        public async Task<ApiResponse<PayActionRes>> PayWithWalletAsync(WalletPaymentDto dto)
        {
            var req = new WalletPayReq("WALLET", dto.PaymentToken, _opt.Integration.Wallet, dto.WalletPhone);
            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/payments/pay", req);

            object? payload = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<object>() : await res.Content.ReadAsStringAsync();
            var success = res.IsSuccessStatusCode;
            var result = new PayActionRes(success, payload);

            return new ApiResponse<PayActionRes> { Status = success, Message = success ? "Wallet payment initiated" : "Wallet payment failed", Data = result, Errors = success ? null : new List<string> { payload?.ToString() ?? "Unknown error" } };
        }

        // ===== Status / Inquiry =====
        public async Task<ApiResponse<OrderStatusDto>> GetStatusAsync(string merchantOrderId)
        {
            var order = (await OrdersRepo.GetAllAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: false)).FirstOrDefault();
            var tx = (await TxRepo.GetAllAsync(t => t.MerchantOrderId == merchantOrderId, trackChanges: false)).OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).FirstOrDefault();
            if (order is null) return new ApiResponse<OrderStatusDto> { Status = false, Message = "Order not found" };

            var dto = new OrderStatusDto(merchantOrderId, order.Status ?? "Unknown", tx?.Status, tx?.IsSuccess ?? false, order.AmountCents, order.Currency, order.PaymobOrderId, tx?.PaymobTransactionId, order.UpdatedAt ?? order.CreatedAt);
            return new ApiResponse<OrderStatusDto> { Status = true, Message = "Status fetched", Data = dto };
        }

        public async Task<ApiResponse<InquiryRes>> InquiryAsync(InquiryDto dto)
        {
            var auth = string.IsNullOrWhiteSpace(dto.AuthToken) ? await _tokenProvider.GetAsync() : dto.AuthToken;

            var url = $"{_opt.ApiBase}/acceptance/transactions/";
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(dto.MerchantOrderId)) qs.Add($"merchant_order_id={Uri.EscapeDataString(dto.MerchantOrderId)}");
            if (dto.OrderId.HasValue) qs.Add($"order_id={dto.OrderId.Value}");
            if (dto.TransactionId.HasValue) qs.Add($"transaction_id={dto.TransactionId.Value}");
            var full = url + (qs.Count > 0 ? $"?{string.Join("&", qs)}" : string.Empty);

            using var req = new HttpRequestMessage(HttpMethod.Get, full);
            req.Headers.Add("Authorization", $"Bearer {auth}");
            var res = await _http.SendAsync(req);

            object? data = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<object>() : await res.Content.ReadAsStringAsync();
            return new ApiResponse<InquiryRes> { Status = true, Message = "Inquiry done", Data = new InquiryRes(data) };
        }

        // ===== Capture / Void / Refund (Step 7) =====
        public async Task<ApiResponse<PayActionRes>> RefundAsync(RefundDto dto)
        {
            var auth = string.IsNullOrWhiteSpace(dto.AuthToken) ? await _tokenProvider.GetAsync() : dto.AuthToken;
            var body = new RefundReq(auth, dto.TransactionId, dto.AmountCents);
            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/void_refund/refund", body);

            object? data = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<object>() : await res.Content.ReadAsStringAsync();
            await ActionRepo.AddAsync(new PaymentAction { ActionType = "Refund", PaymobTransactionId = dto.TransactionId, RequestedAmountCents = dto.AmountCents, Status = res.IsSuccessStatusCode ? "Succeeded" : "Failed", CreatedAt = DateTime.UtcNow });

            if (res.IsSuccessStatusCode)
            {
                var tx = await TxRepo.GetFirstOrDefaultAsync(t => t.PaymobTransactionId == dto.TransactionId, trackChanges: true);
                if (tx != null)
                {
                    tx.RefundedAmountCents += dto.AmountCents;
                    tx.Status = "Refunded";
                    tx.UpdatedAt = DateTime.UtcNow;
                    TxRepo.Update(tx);
                }
            }

            await _uow.SaveChangesAsync();
            var success = res.IsSuccessStatusCode;
            return new ApiResponse<PayActionRes> { Status = success, Message = success ? "Refund succeeded" : "Refund failed", Data = new PayActionRes(success, data), Errors = success ? null : new List<string> { data?.ToString() ?? "Unknown error" } };
        }

        public async Task<ApiResponse<PayActionRes>> VoidAsync(VoidDto dto)
        {
            var auth = string.IsNullOrWhiteSpace(dto.AuthToken) ? await _tokenProvider.GetAsync() : dto.AuthToken;
            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/void_refund/void", new VoidOrCaptureReq(auth, dto.TransactionId, dto.AmountCents));

            object? data = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<object>() : await res.Content.ReadAsStringAsync();
            await ActionRepo.AddAsync(new PaymentAction { ActionType = "Void", PaymobTransactionId = dto.TransactionId, RequestedAmountCents = dto.AmountCents, Status = res.IsSuccessStatusCode ? "Succeeded" : "Failed", CreatedAt = DateTime.UtcNow });

            if (res.IsSuccessStatusCode)
            {
                var tx = await TxRepo.GetFirstOrDefaultAsync(t => t.PaymobTransactionId == dto.TransactionId, trackChanges: true);
                if (tx != null)
                {
                    tx.Status = "Voided";
                    tx.UpdatedAt = DateTime.UtcNow;
                    TxRepo.Update(tx);
                }
            }

            await _uow.SaveChangesAsync();
            var success = res.IsSuccessStatusCode;
            return new ApiResponse<PayActionRes> { Status = success, Message = success ? "Void succeeded" : "Void failed", Data = new PayActionRes(success, data), Errors = success ? null : new List<string> { data?.ToString() ?? "Unknown error" } };
        }

        public async Task<ApiResponse<PayActionRes>> CaptureAsync(CaptureDto dto)
        {
            var auth = string.IsNullOrWhiteSpace(dto.AuthToken) ? await _tokenProvider.GetAsync() : dto.AuthToken;
            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/capture", new VoidOrCaptureReq(auth, dto.TransactionId, dto.AmountCents));

            object? data = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<object>() : await res.Content.ReadAsStringAsync();
            await ActionRepo.AddAsync(new PaymentAction { ActionType = "Capture", PaymobTransactionId = dto.TransactionId, RequestedAmountCents = dto.AmountCents, Status = res.IsSuccessStatusCode ? "Succeeded" : "Failed", CreatedAt = DateTime.UtcNow });

            if (res.IsSuccessStatusCode)
            {
                var tx = await TxRepo.GetFirstOrDefaultAsync(t => t.PaymobTransactionId == dto.TransactionId, trackChanges: true);
                if (tx != null)
                {
                    tx.CapturedAmountCents += dto.AmountCents;
                    tx.Status = "Captured";
                    tx.UpdatedAt = DateTime.UtcNow;
                    TxRepo.Update(tx);
                }
            }

            await _uow.SaveChangesAsync();
            var success = res.IsSuccessStatusCode;
            return new ApiResponse<PayActionRes> { Status = success, Message = success ? "Capture succeeded" : "Capture failed", Data = new PayActionRes(success, data), Errors = success ? null : new List<string> { data?.ToString() ?? "Unknown error" } };
        }

        // ===== Webhook (Source of Truth) =====
        public async Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody)
        {
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in req.Query) fields[kv.Key] = kv.Value.ToString();

            try
            {
                if (req.HasFormContentType)
                {
                    var form = await req.ReadFormAsync();
                    foreach (var kv in form) fields[kv.Key] = kv.Value.ToString();
                }
                else if (!string.IsNullOrWhiteSpace(rawBody) && rawBody.TrimStart().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(rawBody);
                    void Walk(string prefix, JsonElement el)
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Object:
                                foreach (var p in el.EnumerateObject()) Walk(string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}.{p.Name}", p.Value);
                                break;
                            case JsonValueKind.Array:
                                fields[prefix] = el.ToString();
                                break;
                            default:
                                fields[prefix] = el.ToString();
                                break;
                        }
                    }
                    Walk("", doc.RootElement);
                }
            }
            catch { }

            bool valid;
            try { valid = VerifyHmacSha512(fields); } catch { valid = false; }

            if (fields.TryGetValue("source_data.pan", out var pan) && !string.IsNullOrEmpty(pan) && pan.Length > 4)
                fields["source_data.pan"] = new string('X', Math.Max(0, pan.Length - 4)) + pan[^4..];

            await WebhookRepo.AddAsync(new WebhookLog
            {
                RawPayload = rawBody,
                EventType = valid ? "Processed" : "Failure",
                MerchantOrderId = fields.GetValueOrDefault("merchant_order_id"),
                PaymobOrderId = long.TryParse(fields.GetValueOrDefault("order.id"), out var _paymobOrderId) ? _paymobOrderId : (long?)null,
                PaymobTransactionId = long.TryParse(fields.GetValueOrDefault("id"), out var _paymobTxId) ? _paymobTxId : (long?)null,
                IsHmacValid = valid,
                HttpStatus = 200,
                HeadersJson = req.Headers.ToString(),
                ReceivedAt = DateTime.UtcNow,
                IsValid = valid
            });
            await _uow.SaveChangesAsync();

            if (!valid) return new ApiResponse<bool> { Status = false, Message = "Invalid HMAC", Data = false, Errors = new List<string> { "Signature mismatch" } };

            var paymobTxId = TryParseLong(fields, "id");
            var paymobOrderId = TryParseLong(fields, "order.id");
            string? merchantOrderId = fields.TryGetValue("merchant_order_id", out var mo) ? mo : null;
            bool success = TryParseBool(fields, "success");
            bool pending = TryParseBool(fields, "pending");
            long amountCents = TryParseLong(fields, "amount_cents");
            string currency = fields.TryGetValue("currency", out var cur) ? cur ?? _opt.Currency : _opt.Currency;

            if (paymobTxId > 0)
            {
                await UpdateTransactionByPaymobIdAsync(paymobTxId, tx =>
                {
                    tx.PaymobOrderId = (tx.PaymobOrderId is null || tx.PaymobOrderId == 0) ? paymobOrderId : tx.PaymobOrderId;
                    tx.MerchantOrderId = tx.MerchantOrderId ?? merchantOrderId;
                    if (amountCents > 0) tx.AmountCents = amountCents;
                    tx.Currency = currency;
                    tx.IsSuccess = success;
                    tx.Status = success ? "Paid" : (pending ? "Pending" : "Failed");
                    tx.HmacVerified = true;
                });
            }

            if (!string.IsNullOrWhiteSpace(merchantOrderId))
                await UpdateOrderStatusAsync(merchantOrderId, success ? "Paid" : (pending ? "Pending" : "Failed"));

            return new ApiResponse<bool> { Status = true, Message = "Webhook processed", Data = true };
        }

        // ===== HMAC / Helpers =====
        public ApiResponse<bool> VerifyHmac(HmacVerifyDto dto)
        {
            var secret = _opt.HmacSecret ?? throw new InvalidOperationException("HMAC secret not set");
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.MessageToSign));
            var hex = Convert.ToHexString(hash).ToLowerInvariant();

            var ok = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hex), Encoding.UTF8.GetBytes(dto.ReceivedHexSignature.ToLowerInvariant()));
            return new ApiResponse<bool> { Status = ok, Message = ok ? "HMAC valid" : "Invalid HMAC", Data = ok, Errors = ok ? null : new List<string> { "Signature mismatch" } };
        }

        // قفل يمنع تنفيذ متوازي لنفس الـ merchantOrderId
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _orderLocks = new();

        private async Task<IDisposable> AcquireLockAsync(string merchantOrderId)
        {
            var sem = _orderLocks.GetOrAdd(merchantOrderId, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            return new Releaser(() => sem.Release());
        }
        private sealed class Releaser : IDisposable
        {
            private readonly Action _release;
            public Releaser(Action release) => _release = release;
            public void Dispose() => _release();
        }

        // Backoff بسيط ويحترم Retry-After
        private static TimeSpan GetRetryDelay(HttpResponseMessage res, int attempt)
        {
            if (res.Headers.RetryAfter?.Delta is TimeSpan ra && ra > TimeSpan.Zero) return ra;
            return TimeSpan.FromSeconds(Math.Min(8, 2 * attempt)); // 2s,4s,6s...
        }

        // ✅ الريتراي الحقيقي (من غير أي Extension)
        private async Task<HttpResponseMessage> HttpPostJsonWithRetryAsync(string url, object body, int maxRetries = 3, CancellationToken ct = default)
        {
            for (int attempt = 0; ; attempt++)
            {
                var res = await _http.PostAsJsonAsync(url, body, ct);
                if (res.StatusCode != (HttpStatusCode)429) return res;

                if (attempt >= maxRetries) return res; // سيُفشل بعدها EnsureSuccessStatusCode
                var delay = GetRetryDelay(res, attempt + 1);
                _log.LogWarning("429 @ {Url} retry in {Delay}s (attempt {Attempt}/{Max})",
                    url, delay.TotalSeconds, attempt + 1, maxRetries);
                await Task.Delay(delay, ct);
            }
        }
        private async Task<long> GetOrCreatePaymobOrderIdAsync(string merchantOrderId, long amountCents, string currency)
        {
            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true)
                        ?? await UpsertOrderAsync(merchantOrderId, amountCents, currency);

            if (order.PaymobOrderId is long existing && existing > 0)
                return existing;

            var auth = await _tokenProvider.GetAsync();
            var body = new PaymobOrderReq(auth, amountCents, currency, merchantOrderId);

            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/ecommerce/orders", body);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<PaymobOrderRes>();
            var paymobOrderId = data!.id;

            order.PaymobOrderId = paymobOrderId;
            order.Status = "OrderCreated";
            order.UpdatedAt = DateTime.UtcNow;
            OrdersRepo.Update(order);
            await _uow.SaveChangesAsync();

            return paymobOrderId;
        }

        private bool IsKeyValid(PaymentOrder o)
            => !string.IsNullOrWhiteSpace(o.LastPaymentKey)
               && o.PaymentKeyExpiresAt.HasValue
               && o.PaymentKeyExpiresAt.Value > DateTime.UtcNow.AddSeconds(30);

        private async Task<string> GetOrCreatePaymentKeyAsync(
            PaymentOrder order, long amountCents, string currency, BillingData billing, int integrationId, int expirationSeconds = 3600)
        {
            if (IsKeyValid(order))
                return order.LastPaymentKey!;

            var auth = await _tokenProvider.GetAsync();
            var body = new PaymobPaymentKeyReq(auth, amountCents, expirationSeconds, order.PaymobOrderId!.Value, billing, currency, integrationId);

            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/payment_keys", body);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<PaymobPaymentKeyRes>();
            var key = data!.token;

            order.LastPaymentKey = key;
            order.PaymentKeyExpiresAt = DateTime.UtcNow.AddSeconds(expirationSeconds);
            order.Status = "PaymentPending";
            order.UpdatedAt = DateTime.UtcNow;
            OrdersRepo.Update(order);
            await _uow.SaveChangesAsync();

            return key;
        }


        //private async Task<HttpResponseMessage> PostJsonWithRetryAsync(string url, object body, int maxRetries = 3)
        //{
        //    var attempt = 0;
        //    while (true)
        //    {
        //        var res = await HttpPostJsonWithRetryAsync(url, body);

        //        if (res.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
        //            return res;

        //        attempt++;
        //        if (attempt > maxRetries) return res; // سيُفشل بعدها EnsureSuccessStatusCode

        //        // احترم Retry-After إن موجود
        //        TimeSpan delay = TimeSpan.FromSeconds(2 * attempt);
        //        if (res.Headers.RetryAfter?.Delta is TimeSpan ra) delay = ra;
        //        await Task.Delay(delay);
        //    }
        //}

        private bool VerifyHmacSha512(IDictionary<string, string?> fields)
        {
            string[] orderedKeys = new[]
            {
            "amount_cents","created_at","currency","error_occured","has_parent_transaction","id","integration_id",
            "is_3d_secure","is_auth","is_capture","is_refunded","is_standalone_payment","is_voided","order.id",
            "owner","pending","source_data.pan","source_data.sub_type","source_data.type","success"
        };

            var sb = new StringBuilder();
            foreach (var k in orderedKeys) sb.Append(fields.TryGetValue(k, out var v) ? v ?? "" : "");

            var secret = _opt.HmacSecret ?? throw new InvalidOperationException("HMAC secret not set");
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            var received = (fields.TryGetValue("hmac", out var hv) ? hv : "")?.ToLowerInvariant() ?? "";
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hex), Encoding.UTF8.GetBytes(received));
        }
        private async Task<PaymentOrder> UpsertOrderAsync(string merchantOrderId, long amountCents, string currency)
        {
            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);
            if (order is null)
            {
                order = new PaymentOrder { MerchantOrderId = merchantOrderId, AmountCents = amountCents, Currency = currency, Status = "Pending", CreatedAt = DateTime.UtcNow };
                await OrdersRepo.AddAsync(order);
            }
            else
            {
                order.AmountCents = amountCents;
                order.Currency = currency;
                order.UpdatedAt = DateTime.UtcNow;
                OrdersRepo.Update(order);
            }
            await _uow.SaveChangesAsync();
            return order;
        }
        private async Task AddTransactionAsync(string merchantOrderId, long? paymobOrderId, long amountCents, string currency, string integrationType, string status = "Initiated", bool isSuccess = false)
        {
            var tx = new PaymentTransaction { MerchantOrderId = merchantOrderId, PaymobOrderId = paymobOrderId, AmountCents = amountCents, Currency = currency, IntegrationType = integrationType, Status = status, IsSuccess = isSuccess, CreatedAt = DateTime.UtcNow };
            await TxRepo.AddAsync(tx);
            await _uow.SaveChangesAsync();
        }
        private async Task UpdateOrderStatusAsync(string merchantOrderId, string status)
        {
            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;
                OrdersRepo.Update(order);
                await _uow.SaveChangesAsync();
            }
        }
        private async Task UpdateTransactionByPaymobIdAsync(long paymobTransactionId, Action<PaymentTransaction> mutate)
        {
            var tx = await TxRepo.GetFirstOrDefaultAsync(t => t.PaymobTransactionId == paymobTransactionId, trackChanges: true);
            if (tx != null)
            {
                mutate(tx);
                tx.UpdatedAt = DateTime.UtcNow;
                TxRepo.Update(tx);
                await _uow.SaveChangesAsync();
            }
        }
        private async Task<bool> ReserveAndCommitInternalWalletAsync(int userId, long amountCents, string merchantOrderId)
        {
            return true;
        }
        private string EnsureMerchantOrderId(string? merchantOrderId) =>
            string.IsNullOrWhiteSpace(merchantOrderId) ? $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}" : merchantOrderId;
        private string ResolveCurrency(string? c) => string.IsNullOrWhiteSpace(c) ? _opt.Currency : c;
        private static long TryParseLong(IDictionary<string, string?> fields, string key)
        {
            return (fields.TryGetValue(key, out var v) && long.TryParse(v, out var n)) ? n : 0L;
        }
        private static bool TryParseBool(IDictionary<string, string?> fields, string key)
        {
            if (!fields.TryGetValue(key, out var v) || v is null) return false;
            if (bool.TryParse(v, out var b)) return b;

            // بعض الردود بتيجي "1"/"0"
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

    }

    public static class JsonElementExt
    {
        public static string? GetPropertyOrDefault(this JsonElement el, string name) =>
            el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }


}

