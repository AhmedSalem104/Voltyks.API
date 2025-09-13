using Voltyks.Core.DTOs.Paymob.Generic_Result_DTOs;
using Voltyks.Persistence.Entities.Main.Paymob;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;
using Voltyks.Application.Interfaces.Paymob;
using Voltyks.Core.DTOs.Paymob.AddtionDTOs;
using Voltyks.Core.DTOs.Paymob.Input_DTOs;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Core.DTOs.Paymob.Options;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Voltyks.Infrastructure;
using System.Net.Http.Json;
using Voltyks.Core.DTOs;
using System.Text.Json;
using System.Text;
using System.Net;


namespace Voltyks.Application.Services.Paymob
{
    public class PaymobService : IPaymobService
    {
        private readonly HttpClient _http;
        private readonly PaymobOptions _opt;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PaymobService> _log;
        private readonly IPaymobAuthTokenProvider _tokenProvider;
        private readonly IHttpContextAccessor _httpContext;
        private IGenericRepository<PaymentOrder, int> OrdersRepo => _uow.GetRepository<PaymentOrder, int>();
        private IGenericRepository<PaymentTransaction, int> TxRepo => _uow.GetRepository<PaymentTransaction, int>();

        // داخل الـService اللي فيه _uow
        private IGenericRepository<WebhookLog, int> WebhookLogs
            => _uow.GetRepository<WebhookLog, int>();
        private IGenericRepository<PaymentOrder, int> PaymentOrders
            => _uow.GetRepository<PaymentOrder, int>();
        private IGenericRepository<PaymentTransaction, int> PaymentTransactions
            => _uow.GetRepository<PaymentTransaction, int>();
        public PaymobService(HttpClient http, IOptions<PaymobOptions> opt, IUnitOfWork uow, ILogger<PaymobService> log, IPaymobAuthTokenProvider tokenProvider, IHttpContextAccessor httpContext)
        {
            _http = http;
            _opt = opt.Value;
            _uow = uow;
            _log = log;
            _tokenProvider = tokenProvider;
            _httpContext = httpContext;
        }
    
        public async Task<ApiResponse<CardCheckoutResponse>> CheckoutCardAsync(CardCheckoutServiceDto req)
        {
            // (1) Auth-token (مطلوب لاحقًا)
            var _ = await _tokenProvider.GetAsync(); // مجرد ضمان، الاستخدام الفعلي بيتم داخل الخطوات التالية

            // (2) Service-Order (DB) + (3) Paymob Order
            var currency = ResolveCurrency(req.Currency);
            var merchantOrderId = EnsureMerchantOrderId(req.MerchantOrderId);
            using (await AcquireLockAsync(merchantOrderId))
            {
                // upsert في DB كـ "service order"
                var upsert = await UpsertOrderAsync(merchantOrderId, req.AmountCents, currency);
                if (!upsert.Status || upsert.Data is null)
                    return new ApiResponse<CardCheckoutResponse> { Status = false, Message = upsert.Message ?? "Unauthorized" };

                // إنشاء/الحصول على Paymob Order Id
                var paymobOrderId = await GetOrCreatePaymobOrderIdAsync(merchantOrderId, req.AmountCents, currency);
                if (paymobOrderId <= 0)
                    return new ApiResponse<CardCheckoutResponse> { Status = false, Message = "Failed to create Paymob order" };

                // (4) Payment Key (معتمد على الـ integration card)
                var order = upsert.Data;
                var paymentKey = await GetOrCreatePaymentKeyAsync(order, req.AmountCents, currency, req.Billing, _opt.Integration.Card, 3600);

                // (5) iframe-url
                var iframeUrl = BuildCardIframeUrl(paymentKey).Data!;
                var payload = new CardCheckoutResponse(merchantOrderId, paymobOrderId, paymentKey, iframeUrl);

                // سجل Pending أول مرة
                var hasTx = (await TxRepo.GetAllAsync(t => t.MerchantOrderId == merchantOrderId, false)).Any();
                if (!hasTx)
                    await AddTransactionAsync(merchantOrderId, paymobOrderId, req.AmountCents, currency, "Card", "Pending", false);

                return new ApiResponse<CardCheckoutResponse> { Status = true, Message = "Card checkout ready", Data = payload };
            }
        }
        public async Task<ApiResponse<WalletCheckoutResponse>> CheckoutWalletAsync(WalletCheckoutServiceDto req)
        {
            // (1) Auth-token
            var _ = await _tokenProvider.GetAsync();

            // (2) Service-Order (DB) + (3) Paymob Order
            var currency = ResolveCurrency(req.Currency);
            var merchantOrderId = EnsureMerchantOrderId(req.MerchantOrderId);
            using (await AcquireLockAsync(merchantOrderId))
            {
                var upsert = await UpsertOrderAsync(merchantOrderId, req.AmountCents, currency);
                if (!upsert.Status || upsert.Data is null)
                    return new ApiResponse<WalletCheckoutResponse> { Status = false, Message = upsert.Message ?? "Unauthorized" };

                var paymobOrderId = await GetOrCreatePaymobOrderIdAsync(merchantOrderId, req.AmountCents, currency);
                if (paymobOrderId <= 0)
                    return new ApiResponse<WalletCheckoutResponse> { Status = false, Message = "Failed to create Paymob order" };

                // (4) Payment Key (integration wallet) + Billing بسيط
                var billing = new BillingData(
                    first_name: "NA", last_name: "NA", email: "na@example.com", phone_number: req.WalletPhone,
                    apartment: "NA", floor: "NA", building: "NA", street: "NA", city: "Cairo", state: "NA", country: "EG", postal_code: "NA"
                );
                var paymentKey = await GetOrCreatePaymentKeyAsync(upsert.Data, req.AmountCents, currency, billing, _opt.Integration.Wallet, 3600);

                // (5) Wallet-Pay
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
        public async Task<ApiResponse<OrderStatusDto>> GetOrderStatusFromPaymobAsync(long paymobOrderId)
        {
            // 1) Token
            var token = await _tokenProvider.GetAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new ApiResponse<OrderStatusDto>
                {
                    Status = false,
                    Message = "Failed to get auth token",
                    Data = null,
                    Errors = new List<string> { "Auth token is null or empty" }
                };
            }

            // 2) GET order
            var orderUrl = $"{_opt.ApiBase.TrimEnd('/')}/api/ecommerce/orders/{paymobOrderId}";
            using var req = new HttpRequestMessage(HttpMethod.Get, orderUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.ParseAdd("application/json");

            var orderRes = await _http.SendAsync(req);
            if (!orderRes.IsSuccessStatusCode)
            {
                return new ApiResponse<OrderStatusDto>
                {
                    Status = false,
                    Message = $"Paymob error: {(int)orderRes.StatusCode} {orderRes.ReasonPhrase}",
                    Data = null,
                    Errors = new List<string> { "Failed to fetch order from Paymob" }
                };
            }

            var raw = await orderRes.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement.Clone(); // 👈 مهم جداً



            // 3) Basic fields
            string merchantOrderId = root.TryGetProperty("merchant_order_id", out var moid) && moid.ValueKind == JsonValueKind.String
                ? moid.GetString() ?? string.Empty : string.Empty;

            string orderStatus = root.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String
                ? st.GetString() ?? "Unknown" : "Unknown";

            long amountCents = root.TryGetProperty("amount_cents", out var ac) && ac.TryGetInt64(out var a) ? a : 0;
            string currency = root.TryGetProperty("currency", out var cur) && cur.ValueKind == JsonValueKind.String
                ? cur.GetString() ?? "EGP" : "EGP";

            // Flags للمساعدة في الاستنتاج
            long paidAmountCents = root.TryGetProperty("paid_amount_cents", out var pac) && pac.TryGetInt64(out var pacv) ? pacv : 0;
            bool isRefunded = root.TryGetProperty("is_refunded", out var ir) && ir.ValueKind == JsonValueKind.True;
            bool isVoided = root.TryGetProperty("is_voided", out var iv) && iv.ValueKind == JsonValueKind.True;

            // 4) آخر Transaction: من payment_details أو Fallback من transactions list
            string txStatus = "Unknown";
            bool isSuccess = false;
            bool anyPending = false;
            long? txId = null;

            JsonElement? lastTxEl = null;

            if (root.TryGetProperty("payment_details", out var pd) &&
                pd.ValueKind == JsonValueKind.Array &&
                pd.GetArrayLength() > 0)
            {
                lastTxEl = pd.EnumerateArray().OrderBy(x => GetDate(x)).Last();
            }
            else
            {
                // Fallback: GET transactions list
                var txUrl = $"{_opt.ApiBase.TrimEnd('/')}/acceptance/transactions?order_id={paymobOrderId}";
                using var txReq = new HttpRequestMessage(HttpMethod.Get, txUrl);
                txReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                txReq.Headers.Accept.ParseAdd("application/json");

                var txRes = await _http.SendAsync(txReq);
                if (txRes.IsSuccessStatusCode)
                {
                    var txRaw = await txRes.Content.ReadAsStringAsync();
                    using var txDoc = JsonDocument.Parse(txRaw);
                    var txRoot = txDoc.RootElement.Clone(); // 👈 هنا برضه


                    if (txRoot.ValueKind == JsonValueKind.Object &&
                        txRoot.TryGetProperty("results", out var results) &&
                        results.ValueKind == JsonValueKind.Array &&
                        results.GetArrayLength() > 0)
                    {
                        lastTxEl = results.EnumerateArray().OrderBy(x => GetDate(x)).Last();
                    }
                }
            }

            if (lastTxEl.HasValue)
            {
                var lastTx = lastTxEl.Value;

                if (lastTx.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True)
                    isSuccess = true;

                if (lastTx.TryGetProperty("pending", out var p) && p.ValueKind == JsonValueKind.True)
                    anyPending = true;

                if (lastTx.TryGetProperty("id", out var tid) && tid.TryGetInt64(out var t))
                    txId = t;

                txStatus = anyPending ? "Pending" : (isSuccess ? "Paid" : "Failed");
            }

            // 5) استنتاج orderStatus لو Paymob ما رجّعش status نصّي
            if (string.IsNullOrWhiteSpace(orderStatus) || orderStatus == "Unknown")
            {
                if (paidAmountCents >= amountCents && amountCents > 0) orderStatus = "paid";
                else if (isRefunded) orderStatus = "refunded";
                else if (isVoided) orderStatus = "voided";
                else if (anyPending) orderStatus = "pending";
                else orderStatus = "created";
            }

            // 6) Build DTO بنفس ترتيب الـ ctor
            var dto = new OrderStatusDto(
                merchantOrderId,
                orderStatus,
                txStatus,
                isSuccess,
                amountCents,
                currency,
                paymobOrderId,
                txId,
                DateTime.UtcNow
            );

            // 7) Response
            return new ApiResponse<OrderStatusDto>
            {
                Status = true,
                Message = "Order status fetched from Paymob",
                Data = dto,
                Errors = null
            };

            // Helper
            static DateTime GetDate(JsonElement tx)
            {
                if (tx.TryGetProperty("created_at", out var c) && c.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(c.GetString(), out var dt)) return dt;
                if (tx.TryGetProperty("created_at_utc", out var cu) && cu.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(cu.GetString(), out var dtu)) return dtu;
                return DateTime.MinValue;
            }
        }
        public async Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody)
        {

            // [A] Log سريع قبل أي Parsing
            _log?.LogWarning("Webhook arrived: {Method} {Path}{Query} UA={UA}",
                req.Method, req.Path, req.QueryString.Value, req.Headers["User-Agent"].ToString());

            // --------- 0) تجميع الحقول كما هي عندك ----------
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in req.Query) fields[kv.Key] = kv.Value.ToString();

            if (req.Headers.TryGetValue("hmac", out var h1)) fields["hmac"] = h1.ToString();
            else if (req.Headers.TryGetValue("hmac_signature", out var h2)) fields["hmac"] = h2.ToString();
            else if (req.Headers.TryGetValue("X-HMAC-Signature", out var h3)) fields["hmac"] = h3.ToString();
            // [B] محاولات إضافية لأسماء HMAC بديلة (تشخيص فقط)
            if (!fields.ContainsKey("hmac"))
            {
                if (req.Headers.TryGetValue("secure-hash", out var h4)) fields["hmac"] = h4.ToString();
                else if (req.Headers.TryGetValue("X-Signature", out var h5)) fields["hmac"] = h5.ToString();
            }

            try
            {
                if (req.HasFormContentType)
                {
                    var form = await req.ReadFormAsync();
                    foreach (var kv in form) fields[kv.Key] = kv.Value.ToString();
                    if (!fields.ContainsKey("hmac") && form.TryGetValue("hmac", out var fh)) fields["hmac"] = fh.ToString();
                }
                else if (!string.IsNullOrWhiteSpace(rawBody) && rawBody.TrimStart().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(rawBody);
                    void Walk(string prefix, JsonElement el)
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Object:
                                foreach (var p in el.EnumerateObject())
                                    Walk(string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}.{p.Name}", p.Value);
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
                    if (!fields.ContainsKey("hmac") && doc.RootElement.TryGetProperty("hmac", out var hJson) && hJson.ValueKind == JsonValueKind.String)
                        fields["hmac"] = hJson.GetString();
                }
            }
            catch { /* ignore parsing errors */ }

            // --------- 1) Logging (اختياري) ----------
            try
            {
                _log?.LogInformation("====== PAYMOB WEBHOOK FIELDS ======\n{pairs}\n==================================",
                    string.Join("\n", fields.Select(kv => $"{kv.Key} = {kv.Value}")));
            }
            catch { }

            // --------- 2) لازم HMAC ----------
            if (!fields.ContainsKey("hmac") || string.IsNullOrWhiteSpace(fields["hmac"]))
            {
                await WebhookLogs.AddAsync(new WebhookLog
                {
                    RawPayload = rawBody,
                    EventType = "NoHmac-Debug",
                    HeadersJson = req.Headers.ToString(),
                    HttpStatus = 200,
                    ReceivedAt = DateTime.UtcNow,
                    IsHmacValid = false
                });
                await _uow.SaveChangesAsync();

                return new ApiResponse<bool> { Status = true, Message = "No HMAC (debug ack)", Data = true };
            }

            // --------- 3) Mask PAN ----------
            if (fields.TryGetValue("source_data.pan", out var pan) && !string.IsNullOrEmpty(pan) && pan.Length > 4)
                fields["source_data.pan"] = new string('X', Math.Max(0, pan.Length - 4)) + pan[^4..];

            // --------- 4) HMAC Verify ----------
            bool valid;
            try { valid = VerifyHmacSha512(fields); } catch { valid = false; }

            await WebhookLogs.AddAsync(new WebhookLog
            {
                RawPayload = rawBody,
                EventType = valid ? "Received" : "Failure",
                MerchantOrderId = fields.GetValueOrDefault("merchant_order_id"),
                PaymobOrderId = TryParseLong(fields, "order.id"),
                PaymobTransactionId = TryParseLong(fields, "id"),
                IsHmacValid = valid,
                HttpStatus = valid ? 200 : 400,
                HeadersJson = req.Headers.ToString(),
                ReceivedAt = DateTime.UtcNow,
                IsValid = valid
            });
            await _uow.SaveChangesAsync();

            if (!valid)
            {
                return new ApiResponse<bool>
                {
                    Status = false,
                    Message = "Invalid HMAC",
                    Data = false,
                    Errors = new List<string> { "Signature mismatch" }
                };
            }

            // --------- 5) Routing by type ----------
            var eventType = fields.GetValueOrDefault("type")?.Trim()?.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(eventType)) eventType = "TRANSACTION";

            switch (eventType)
            {
                case "TRANSACTION":
                    {
                        var paymobTxId = TryParseLong(fields, "id");

                        // Idempotency
                        if (paymobTxId > 0 && await ExistsProcessedTxAsync(paymobTxId))
                            return new ApiResponse<bool> { Status = true, Message = "Duplicate event ignored (idempotent)", Data = true };

                        var paymobOrderId = TryParseLong(fields, "order.id");
                        var merchantOrderId = fields.GetValueOrDefault("merchant_order_id");

                        bool isRefunded = TryParseBool(fields, "is_refunded");
                        bool isVoided = TryParseBool(fields, "is_voided");
                        bool isCapture = TryParseBool(fields, "is_capture");
                        bool success = TryParseBool(fields, "success");
                        bool pending = TryParseBool(fields, "pending");

                        long amountCents = TryParseLong(fields, "amount_cents");
                        string currency = fields.TryGetValue("currency", out var cur) ? (cur ?? _opt.Currency) : _opt.Currency;

                        string localStatus =
                            isRefunded ? "Refunded" :
                            isVoided ? "Voided" :
                            isCapture ? "Captured" :
                            pending ? "Pending" :
                            success ? "Paid" : "Failed";

                        // تحديث الـTransaction (لو موجودة)
                        if (paymobTxId > 0)
                        {
                            await UpdateTransactionByPaymobIdAsync(paymobTxId, tx =>
                            {
                                tx.PaymobOrderId = (tx.PaymobOrderId is null || tx.PaymobOrderId == 0) ? paymobOrderId : tx.PaymobOrderId;
                                tx.MerchantOrderId = tx.MerchantOrderId ?? merchantOrderId;
                                if (amountCents > 0) tx.AmountCents = amountCents;
                                tx.Currency = currency;
                                tx.IsSuccess = success;
                                tx.Status = localStatus;
                                tx.HmacVerified = true;
                            });
                        }

                        // تحديث حالة الـOrder
                        if (!string.IsNullOrWhiteSpace(merchantOrderId))
                            await UpdateOrderStatusAsync(merchantOrderId!, localStatus);

                        // علّم الحدث كمُعالج (Idempotency mark)
                        if (paymobTxId > 0)
                            await MarkProcessedTxAsync(paymobTxId, "TRANSACTION_PROCESSED");

                        return new ApiResponse<bool> { Status = true, Message = "Webhook processed (transaction)", Data = true };
                    }

                case "TOKEN":
                    {
                        await MarkGenericAsync("TOKEN", fields.GetValueOrDefault("owner") ?? "unknown", true);
                        return new ApiResponse<bool> { Status = true, Message = "Webhook processed (token)", Data = true };
                    }

                default:
                    {
                        await MarkGenericAsync(eventType, fields.GetValueOrDefault("id") ?? Guid.NewGuid().ToString("N"), true);
                        return new ApiResponse<bool> { Status = true, Message = $"Webhook processed ({eventType})", Data = true };
                    }
            }
        }

        public async Task<ApiResponse<IntentionClientSecretDto>> ExchangePaymentKeyForClientSecretAsync(string paymentKey, string? publicKeyOverride = null, CancellationToken ct = default)
        {
            var token = await GenerateBearerTokenAsync();

            if (string.IsNullOrWhiteSpace(paymentKey))
                return new ApiResponse<IntentionClientSecretDto> { Status = false, Message = "paymentKey is required" };

            var publicKey = string.IsNullOrWhiteSpace(publicKeyOverride) ? _opt.PublicKey : publicKeyOverride;
            if (string.IsNullOrWhiteSpace(publicKey))
                return new ApiResponse<IntentionClientSecretDto> { Status = false, Message = "publicKey is required (config or request)" };

            var url = !string.IsNullOrWhiteSpace(_opt.Intention?.Url)
                ? _opt.Intention!.Url!
                : $"{_opt.ApiBase?.TrimEnd('/')}{_opt.Intention?.Path}";

            if (string.IsNullOrWhiteSpace(url))
                return new ApiResponse<IntentionClientSecretDto> { Status = false, Message = "Intention endpoint URL is not configured" };

            var body = new
            {
                payment_key = paymentKey,
                public_key = publicKey
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);

            // حساب HMAC وتعيينه في الهيدر
            var hmacSignature = CalculateHmac(body, _opt.HmacSecret);
            Console.WriteLine($"HMAC Signature: {hmacSignature}"); // قم بطباعة التوقيع لمقارنة

            req.Headers.Add("X-HMAC-Signature", hmacSignature);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.ParseAdd("application/json");
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning("Intention API error: {Status} - {Body}", (int)res.StatusCode, raw);
                return new ApiResponse<IntentionClientSecretDto>
                {
                    Status = false,
                    Message = $"Intention API error: {(int)res.StatusCode}",
                    Errors = new List<string> { raw }
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                var clientSecret = root.GetProperty("client_secret").GetString();

                return new ApiResponse<IntentionClientSecretDto>
                {
                    Status = true,
                    Message = "Token generated successfully",
                    Data = new IntentionClientSecretDto(clientSecret!)
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to parse Intention response: {Body}", raw);
                return new ApiResponse<IntentionClientSecretDto>
                {
                    Status = false,
                    Message = "Failed to parse Intention response",
                    Errors = new List<string> { ex.Message, raw }
                };
            }
        }
        public async Task<string> GenerateBearerTokenAsync()
        {
            var url = "https://accept.paymob.com/api/auth/tokens";
            var body = new
            {
                api_key = _opt.ApiKey // تأكد من أن api_key في الإعدادات صالح
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            var res = await _http.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                return root.GetProperty("token").GetString(); // إرجاع التوكن
            }
            else
            {
                // طباعة الخطأ إذا فشل الحصول على التوكن
                _log.LogError("Failed to get Bearer Token: {Error}", raw);
                throw new Exception($"Failed to get Bearer Token: {raw}");
            }
        }
        public string CalculateHmac(object body, string secret)
        {

         

            var bodyString = JsonSerializer.Serialize(body);
            Console.WriteLine("Body String: " + bodyString); // طباعة الـ body الذي سيتم حساب التوقيع عليه

            using var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(secret));  // حساب الـ HMAC باستخدام الـ secret
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(bodyString)); // حساب التوقيع

            var hmacSignature = BitConverter.ToString(hash).Replace("-", "").ToLower(); // تحويل الـ hash إلى String
            Console.WriteLine("HMAC Signature: " + hmacSignature); // طباعة الـ HMAC Signature

            return hmacSignature;  // إرجاع التوقيع المحسوب

        }
        public void CompareHmacSignature(string calculatedSignature, string receivedSignature)
        {
            if (calculatedSignature == receivedSignature)
            {
                Console.WriteLine("The signatures match!");
            }
            else
            {
                Console.WriteLine("The signatures do not match!");
            }
        }



        private async Task<bool> ExistsProcessedTxAsync(long paymobTransactionId)
        {
            return await WebhookLogs.AnyAsync(w =>
                w.PaymobTransactionId == paymobTransactionId &&
                w.EventType == "TRANSACTION_PROCESSED" &&
                w.IsHmacValid && w.IsValid);
        }
        private async Task MarkProcessedTxAsync(long paymobTransactionId, string evtType)
        {
            await WebhookLogs.AddAsync(new WebhookLog
            {
                EventType = evtType, // "TRANSACTION_PROCESSED"
                PaymobTransactionId = paymobTransactionId,
                IsHmacValid = true,
                IsValid = true,
                HttpStatus = 200,
                ReceivedAt = DateTime.UtcNow
            });
            await _uow.SaveChangesAsync();
        }
        private async Task MarkGenericAsync(string evtType, string key, bool ok)
        {
            await WebhookLogs.AddAsync(new WebhookLog
            {
                EventType = evtType,
                MerchantOrderId = key,
                IsHmacValid = true,
                IsValid = ok,
                HttpStatus = 200,
                ReceivedAt = DateTime.UtcNow
            });
            await _uow.SaveChangesAsync();
        }
        // ===== تحديث الطلب =====
        private async Task UpdateOrderStatusAsync(string merchantOrderId, string status)
        {
            var order = await PaymentOrders.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);
            if (order is null) return;

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            PaymentOrders.Update(order);
            await _uow.SaveChangesAsync();
        }
        // ===== تحديث المعاملة =====
        private async Task UpdateTransactionByPaymobIdAsync(long paymobTransactionId, Action<PaymentTransaction> mutate)
        {
            var tx = await PaymentTransactions.GetFirstOrDefaultAsync(t => t.PaymobTransactionId == paymobTransactionId, trackChanges: true);
            if (tx is null) return;

            mutate(tx);
            tx.UpdatedAt = DateTime.UtcNow;

            PaymentTransactions.Update(tx);
            await _uow.SaveChangesAsync();
        }


        // ================== Helpers / Infra ==================
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
        private static TimeSpan GetRetryDelay(HttpResponseMessage res, int attempt)
        {
            if (res.Headers.RetryAfter?.Delta is TimeSpan ra && ra > TimeSpan.Zero) return ra;
            return TimeSpan.FromSeconds(Math.Min(8, 2 * attempt));
        }
        private async Task<HttpResponseMessage> HttpPostJsonWithRetryAsync(string url, object body, int maxRetries = 3, CancellationToken ct = default)
        {
            for (int attempt = 0; ; attempt++)
            {
                var res = await _http.PostAsJsonAsync(url, body, ct);
                if (res.StatusCode != (HttpStatusCode)429) return res;

                if (attempt >= maxRetries) return res;
                var delay = GetRetryDelay(res, attempt + 1);
                _log.LogWarning("429 @ {Url} retry in {Delay}s (attempt {Attempt}/{Max})",
                    url, delay.TotalSeconds, attempt + 1, maxRetries);
                await Task.Delay(delay, ct);
            }
        }
        private async Task<long> GetOrCreatePaymobOrderIdAsync(string merchantOrderId, long amountCents, string currency)
        {
            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);

            if (order is null)
            {
                var upsertResponse = await UpsertOrderAsync(merchantOrderId, amountCents, currency);
                if (!upsertResponse.Status || upsertResponse.Data is null)
                    return 0;
                order = upsertResponse.Data;
            }

            if (order.PaymobOrderId is long existing && existing > 0)
                return existing;

            var auth = await _tokenProvider.GetAsync();

            var body = new PaymobOrderReq()
            {
                auth_token = auth,
                //delivery_needed = false,
                amount_cents = amountCents,
                currency = currency,
                merchant_order_id = merchantOrderId,
                //items = Array.Empty<object>()
            };

            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/ecommerce/orders", body);
            //res.EnsureSuccessStatusCode();
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
        private async Task<string> GetOrCreatePaymentKeyAsync(PaymentOrder order, long amountCents, string currency, BillingData billing, int integrationId, int expirationSeconds = 3600)
        {
            if (IsKeyValid(order))
                return order.LastPaymentKey!;

            var auth = await _tokenProvider.GetAsync();
            var norm = NormalizeAndValidateBilling(billing);
            if (!norm.IsValid)
                throw new InvalidOperationException($"Invalid billing_data: {norm.Error}");

            var body = new PaymobPaymentKeyReq(auth, amountCents, expirationSeconds, order.PaymobOrderId!.Value, billing, currency, integrationId);

            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/acceptance/payment_keys", body);
            //res.EnsureSuccessStatusCode();
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
        private ApiResponse<string> BuildCardIframeUrl(string paymentKey)
        {
            var url = $"https://accept.paymob.com/api/acceptance/iframes/{_opt.IframeId}?payment_token={paymentKey}";
            return new ApiResponse<string> { Status = true, Message = "iFrame URL ready", Data = url };
        }
        private async Task<ApiResponse<PayActionRes>> PayWithWalletAsync(WalletPaymentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PaymentToken))
                return new ApiResponse<PayActionRes> { Status = false, Message = "paymentToken is required" };

            var phone = dto.WalletPhone?.Trim();
            if (string.IsNullOrWhiteSpace(phone) || !phone.StartsWith("01"))
                return new ApiResponse<PayActionRes> { Status = false, Message = "walletPhone must start with 01" };

            var req = new WalletPayReq
            {
                PaymentToken = dto.PaymentToken,
                Source = new WalletSource { Identifier = phone!, Subtype = "WALLET" }
            };

            var url = $"{_opt.ApiBase}/acceptance/payments/pay";
            var res = await HttpPostJsonWithRetryAsync(url, req);

            object? payload = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<object>() : await res.Content.ReadAsStringAsync();
            var success = res.IsSuccessStatusCode;
            var result = new PayActionRes(success, payload);

            return new ApiResponse<PayActionRes>
            {
                Status = success,
                Message = success ? "Wallet payment initiated" : "Wallet payment failed",
                Data = result,
                Errors = success ? null : new List<string> { payload?.ToString() ?? "Unknown error" }
            };
        }
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
        private async Task<ApiResponse<PaymentOrder>> UpsertOrderAsync(string merchantOrderId, long amountCents, string currency)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId is null)
                return new ApiResponse<PaymentOrder>(null, "Unauthorized", false);

            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);

            if (order is null)
            {
                order = new PaymentOrder
                {
                    MerchantOrderId = merchantOrderId,
                    AmountCents = amountCents,
                    Currency = currency,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UserId = currentUserId
                };
                await OrdersRepo.AddAsync(order);
            }
            else
            {
                order.AmountCents = amountCents;
                order.Currency = currency;
                order.UpdatedAt = DateTime.UtcNow;

                if (order.UserId != currentUserId)
                    order.UserId = currentUserId;

                OrdersRepo.Update(order);
            }

            await _uow.SaveChangesAsync();

            return new ApiResponse<PaymentOrder>(order, "Order upserted successfully", true);
        }
        private string? GetCurrentUserId()
            => _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private (bool IsValid, string Error, object Value) NormalizeAndValidateBilling(BillingData b)
        {
            if (b == null) return (false, "billing_data is required", new { });

            bool missing =
                string.IsNullOrWhiteSpace(b.first_name) ||
                string.IsNullOrWhiteSpace(b.last_name) ||
                string.IsNullOrWhiteSpace(b.email) ||
                string.IsNullOrWhiteSpace(b.phone_number);

            if (missing) return (false, "first_name, last_name, email, phone_number are required", new { });

            string S(string? x) => string.IsNullOrWhiteSpace(x) ? "NA" : x.Trim();

            var obj = new
            {
                apartment = "NA",
                floor = "NA",
                building = "NA",
                street = "NA",
                city = "NA",
                state = "NA",
                country = "NA",
                email = S(b.email),
                phone_number = S(b.phone_number),
                first_name = S(b.first_name),
                last_name = S(b.last_name),
                postal_code = "NA"
            };
            return (true, "", obj);
        }

        private async Task AddTransactionAsync(string merchantOrderId, long? paymobOrderId, long amountCents, string currency, string integrationType, string status = "Initiated", bool isSuccess = false)
        {
            var tx = new PaymentTransaction
            {
                MerchantOrderId = merchantOrderId,
                PaymobOrderId = paymobOrderId,
                AmountCents = amountCents,
                Currency = currency,
                IntegrationType = integrationType,
                Status = status,
                IsSuccess = isSuccess,
                CreatedAt = DateTime.UtcNow
            };
            await TxRepo.AddAsync(tx);
            await _uow.SaveChangesAsync();
        }
        //private async Task UpdateOrderStatusAsync(string merchantOrderId, string status)
        //{
        //    var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);
        //    if (order != null)
        //    {
        //        order.Status = status;
        //        order.UpdatedAt = DateTime.UtcNow;
        //        OrdersRepo.Update(order);
        //        await _uow.SaveChangesAsync();
        //    }
        //}
        //private async Task UpdateTransactionByPaymobIdAsync(long paymobTransactionId, Action<PaymentTransaction> mutate)
        //{
        //    var tx = await TxRepo.GetFirstOrDefaultAsync(t => t.PaymobTransactionId == paymobTransactionId, trackChanges: true);
        //    if (tx != null)
        //    {
        //        mutate(tx);
        //        tx.UpdatedAt = DateTime.UtcNow;
        //        TxRepo.Update(tx);
        //        await _uow.SaveChangesAsync();
        //    }
        //}
        private string EnsureMerchantOrderId(string? merchantOrderId)
         => string.IsNullOrWhiteSpace(merchantOrderId)
         ? Guid.NewGuid().ToString("N")
         : merchantOrderId;
        private string ResolveCurrency(string? c) => string.IsNullOrWhiteSpace(c) ? _opt.Currency : c;
        private static long TryParseLong(IDictionary<string, string?> fields, string key)
            => (fields.TryGetValue(key, out var v) && long.TryParse(v, out var n)) ? n : 0L;
        private static bool TryParseBool(IDictionary<string, string?> fields, string key)
        {
            if (!fields.TryGetValue(key, out var v) || v is null) return false;
            if (bool.TryParse(v, out var b)) return b;
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
























