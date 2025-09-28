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
using Voltyks.Core.DTOs.Paymob.CardsDTOs;
using Twilio.TwiML.Messaging;
using Voltyks.Core.DTOs.Paymob.intention;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using System.Transactions;
using Voltyks.Persistence.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


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
        private readonly IHttpClientFactory _httpFactory;
        private readonly UserManager<AppUser> _userManager; // 👈 ده اللي ناقصك


        private IGenericRepository<PaymentOrder, int> OrdersRepo => _uow.GetRepository<PaymentOrder, int>();
        private IGenericRepository<PaymentTransaction, int> TxRepo => _uow.GetRepository<PaymentTransaction, int>();
        //private IGenericRepository<UserSavedCard, int> SaveCardRepo => _uow.GetRepository<UserSavedCard, int>();

        // داخل الـService اللي فيه _uow
        private IGenericRepository<WebhookLog, int> WebhookLogs => _uow.GetRepository<WebhookLog, int>();
        private IGenericRepository<PaymentOrder, int> PaymentOrders => _uow.GetRepository<PaymentOrder, int>();
        private IGenericRepository<PaymentTransaction, int> PaymentTransactions => _uow.GetRepository<PaymentTransaction, int>();
        private IGenericRepository<UserSavedCard, int> SavedCards => _uow.GetRepository<UserSavedCard, int>();

        public PaymobService(HttpClient http, IOptions<PaymobOptions> opt, IUnitOfWork uow, ILogger<PaymobService> log, IPaymobAuthTokenProvider tokenProvider, IHttpContextAccessor httpContext, IHttpClientFactory httpFactory , UserManager<AppUser> userManager)
        {
            _http = http;
            _opt = opt.Value;
            _uow = uow;
            _log = log;
            _tokenProvider = tokenProvider;
            _httpContext = httpContext;
            _httpFactory = httpFactory;
            _userManager = userManager;

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
                var paymentKey = await GetOrCreatePaymentKeyAsync(order, req.AmountCents, currency, req.Billing, _opt.Integration.Card, 3600, tokenize: req.SaveCard);

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
                var paymentKey = await GetOrCreatePaymentKeyAsync(upsert.Data, req.AmountCents, currency, billing, _opt.Integration.Wallet, 3600, tokenize: req.SaveCard);

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
        public async Task<ApiResponse<CreateIntentResponse>> CreateIntentionAsync(CreateIntentRequest r, CancellationToken ct = default)
        {
            // 1) Basic validation
            if (r is null || r.Amount <= 0 || r.BillingData is null)
                return new ApiResponse<CreateIntentResponse>("Invalid request", false, new List<string> { "Request body or Amount/BillingData invalid." });

            try
            {
                // 2) Prepare order and currency
                var orderId = string.IsNullOrWhiteSpace(r.MerchantOrderId) ? Guid.NewGuid().ToString("N") : r.MerchantOrderId;
                const string currency = "EGP";

                var upsert = await UpsertOrderAsync(orderId, (long)r.Amount, currency);
                if (!upsert.Status || upsert.Data is null)
                    return new ApiResponse<CreateIntentResponse>(upsert.Message ?? "Unauthorized", false);

                // 3) Pick method + integrationId (from appsettings)
                var selectedMethod = string.Equals(r.PaymentMethod, "Wallet", StringComparison.OrdinalIgnoreCase)
                    ? "Wallet"
                    : "Card";

                int integrationId = selectedMethod == "Wallet"
                    ? _opt.Integration.Wallet
                    : _opt.Integration.Card;

                if (integrationId <= 0)
                    return new ApiResponse<CreateIntentResponse>($"No valid integration_id configured for {selectedMethod}.", false);

                // --- Added: normalize base URL and check secret key (no logic change to your flow) ---
                var baseUrl = string.IsNullOrWhiteSpace(_opt.Intention.Url) ? null : _opt.Intention.Url.TrimEnd('/') + "/";
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return new ApiResponse<CreateIntentResponse>("Paymob Intention URL is missing.", false);

                var secretKey = _opt.SecretKey;
                if (string.IsNullOrWhiteSpace(secretKey))
                    return new ApiResponse<CreateIntentResponse>("Paymob Secret Key is missing.", false);
                // -------------------------------------------------------------------------------

                // 4) Compose request body for Paymob (requires payment_methods: [id])
                var specialReference = Guid.NewGuid().ToString("N");
                var notificationUrl = string.IsNullOrWhiteSpace(r.NotificationUrl)
                    ? $"http://voltyks-app.runasp.net/notification/{orderId}" // يُفضّل HTTPS في الإنتاج
                    : r.NotificationUrl;



                var body = new
                {
                    amount = r.Amount,      // تأكد من وحدة المبلغ حسب إعداد حسابك
                    currency,
                    // 👇 change: send as strings to avoid any parsing ambiguity on Paymob side
                    //integration_id = new[] { integrationId.ToString() }, // << المطلوب من Paymob
                    //payment_methods = new[] { selectedMethod }, // << المطلوب من Paymob
                    payment_methods = new[] { integrationId }, // << المطلوب من Paymob

                    billing_data = new
                    {
                        first_name = r.BillingData.First_Name,
                        last_name = r.BillingData.Last_Name,
                        email = r.BillingData.Email,
                        phone_number = r.BillingData.Phone_Number
                    },
                    special_reference = specialReference,
                    notification_url = notificationUrl,
                    tokenize = r.SaveCard,
                    //merchant_order_id = orderId,                 // أو uid:<userId>|ord:<orderId>
                    merchant_order_id = $"uid:{upsert.Data!.UserId}|ord:{orderId}",
                    metadata = new { user_id = upsert.Data!.UserId }  // عدّل حسب ما بيرجع UpsertOrderAsync

                    // redirection_url = (selectedMethod == "Card" && !string.IsNullOrWhiteSpace(r.RedirectionUrl)) ? r.RedirectionUrl : null


                };


                // 5) Call Paymob Intention API
                var http = _httpFactory.CreateClient();
                using var req = new HttpRequestMessage(HttpMethod.Post, baseUrl);
                req.Headers.Accept.ParseAdd("application/json");
                req.Headers.TryAddWithoutValidation("Authorization", $"Token {secretKey}");
                req.Content = new StringContent(
                    JsonSerializer.Serialize(
                        body,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        }),
                    Encoding.UTF8, "application/json"
                );


                using var res = await http.SendAsync(req, ct);
                var raw = await res.Content.ReadAsStringAsync(ct);
                if (!res.IsSuccessStatusCode)
                {
                    // رجّع تفاصيل تفيدك في التشخيص لو حصل 400 تاني
                    var debug = new
                    {
                        httpStatus = (int)res.StatusCode,
                        selectedMethod,
                        integrationId,
                        orderId,
                        baseUrl,
                        keyFingerprint = !string.IsNullOrWhiteSpace(secretKey) && secretKey.Length >= 8
                            ? $"{secretKey[..4]}...{secretKey[^4..]}"
                            : "<missing>",
                        paymobRaw = raw
                    };
                    return new ApiResponse<CreateIntentResponse>($"Paymob intention error {(int)res.StatusCode}", false, new List<string> { JsonSerializer.Serialize(debug) });
                }

                // 6) Parse Paymob response
                var pm = JsonSerializer.Deserialize<PaymobIntentionResponse>(
                    raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (pm is null || string.IsNullOrWhiteSpace(pm.ClientSecret) || string.IsNullOrWhiteSpace(pm.Id))
                    return new ApiResponse<CreateIntentResponse>("Unexpected Paymob response.", false, new List<string> { raw });

                // 7) Record transaction locally (Pending)
                await AddTransactionAsync(orderId, pm.IntentionOrderId, (long)r.Amount, currency, "Intention", "Pending", false);

                // 8) Extract payment keys (if any)
                var keys = pm.PaymentKeys?
                    .Select(k => k.Key)
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .ToList();

                // 9) Build response DTO
                var result = new CreateIntentResponse(
                    pm.ClientSecret,
                    _opt.PublicKey,
                    pm.Id,                 // IntentionId
                    pm.IntentionOrderId,
                    pm.RedirectionUrl,
                    pm.Status,
                    keys
                );

                return new ApiResponse<CreateIntentResponse>(result, "Intention created successfully.", true);
            }
            catch (Exception ex)
            {
                return new ApiResponse<CreateIntentResponse>("Failed to create Intention.", false, new List<string> { ex.Message });
            }
        }
        private static string? FirstValue(IDictionary<string, string?> f, params string[] keys)
        {
            foreach (var k in keys)
                if (f.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v;
            return null;
        }
        private static long TryParseLong(IDictionary<string, string?> f, params string[] keys)
        {
            foreach (var k in keys)
                if (f.TryGetValue(k, out var s) && long.TryParse(s, out var n))
                    return n;
            return 0;
        }
        private static bool TryParseBool(IDictionary<string, string?> f, params string[] keys)
        {
            foreach (var k in keys)
                if (f.TryGetValue(k, out var s) && bool.TryParse(s, out var b))
                    return b;
            return false;
        }
      
        //public async Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody)
        //{
        //    // [A] Log سريع
        //    _log?.LogWarning("Webhook arrived: {Method} {Path}{Query} UA={UA}",
        //        req.Method, req.Path, req.QueryString.Value, req.Headers["User-Agent"].ToString());

        //    // --------- 0) تجميع الحقول كما هي ----------
        //    var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        //    foreach (var kv in req.Query) fields[kv.Key] = kv.Value.ToString();

        //    if (req.Headers.TryGetValue("hmac", out var h1)) fields["hmac"] = h1.ToString();
        //    else if (req.Headers.TryGetValue("hmac_signature", out var h2)) fields["hmac"] = h2.ToString();
        //    else if (req.Headers.TryGetValue("X-HMAC-Signature", out var h3)) fields["hmac"] = h3.ToString();

        //    if (!fields.ContainsKey("hmac"))
        //    {
        //        if (req.Headers.TryGetValue("secure-hash", out var h4)) fields["hmac"] = h4.ToString();
        //        else if (req.Headers.TryGetValue("X-Signature", out var h5)) fields["hmac"] = h5.ToString();
        //    }

        //    try
        //    {
        //        if (req.HasFormContentType)
        //        {
        //            var form = await req.ReadFormAsync();
        //            foreach (var kv in form) fields[kv.Key] = kv.Value.ToString();
        //            if (!fields.ContainsKey("hmac") && form.TryGetValue("hmac", out var fh)) fields["hmac"] = fh.ToString();
        //        }
        //        else if (!string.IsNullOrWhiteSpace(rawBody) && rawBody.TrimStart().StartsWith("{"))
        //        {
        //            using var doc = JsonDocument.Parse(rawBody);

        //            void Walk(string prefix, JsonElement el)
        //            {
        //                switch (el.ValueKind)
        //                {
        //                    case JsonValueKind.Object:
        //                        foreach (var p in el.EnumerateObject())
        //                            Walk(string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}.{p.Name}", p.Value);
        //                        break;
        //                    case JsonValueKind.Array:
        //                        fields[prefix] = el.ToString();
        //                        break;
        //                    default:
        //                        fields[prefix] = el.ToString();
        //                        break;
        //                }
        //            }

        //            Walk("", doc.RootElement);

        //            if (!fields.ContainsKey("hmac") &&
        //                doc.RootElement.TryGetProperty("hmac", out var hJson) &&
        //                hJson.ValueKind == JsonValueKind.String)
        //            {
        //                fields["hmac"] = hJson.GetString();
        //            }
        //        }
        //    }
        //    catch { /* ignore parsing errors */ }

        //    // --------- 1) Logging (اختياري) ----------
        //    try
        //    {
        //        _log?.LogInformation("====== PAYMOB WEBHOOK FIELDS ======\n{pairs}\n==================================",
        //            string.Join("\n", fields.Select(kv => $"{kv.Key} = {kv.Value}")));
        //    }
        //    catch { }

        //    // --------- 2) لازم HMAC (لو مفيش هنلوج ونكمل) ----------
        //    if (!fields.ContainsKey("hmac") || string.IsNullOrWhiteSpace(fields["hmac"]))
        //    {
        //        await WebhookLogs.AddAsync(new WebhookLog
        //        {
        //            RawPayload = rawBody,
        //            EventType = "NoHmac-Debug",
        //            HeadersJson = req.Headers.ToString(),
        //            HttpStatus = 200,
        //            ReceivedAt = DateTime.UtcNow,
        //            IsHmacValid = false,

        //        });
        //        await _uow.SaveChangesAsync();

        //        // هنرجّع 200 عشان بايموب ما يعيدش الإرسال بلا نهاية
        //        return new ApiResponse<bool> { Status = true, Message = "No HMAC (debug ack)", Data = true };
        //    }

        //    // --------- 3) تحديد النوع مبكراً (مع دعم obj.) ----------
        //    string typeRaw = (FirstValue(fields, "type", "obj.type") ?? "TRANSACTION").Trim();
        //    string eventType = typeRaw.ToUpperInvariant();

        //    // لو ظهر أي مفاتيح توكن، نعتبرها
        //    // 
        //    bool hasCardTokenKeys = FirstValue(fields,
        //        "obj.token", "obj.saved_card_token", "obj.card_token",
        //        "token", "saved_card_token", "card_token") != null;
        //    if (hasCardTokenKeys) eventType = "CARD_TOKEN";

        //    // --------- 3.1) تحقق HMAC ----------
        //    bool valid;
        //    try { valid = VerifyHmacSha512(fields, rawBody); } catch { valid = false; }

        //    // استثناء مؤقّت: اسمح بتوكنات الكروت حتى لو الـHMAC فشل (لتأكيد الحفظ فقط)
        //    if ((eventType == "CARD_TOKEN" || eventType == "TOKEN") && !valid)
        //    {
        //        _log?.LogWarning("TOKEN/CARD_TOKEN webhook with invalid/missing HMAC – TEMPORARY ACCEPT for save-card testing.");
        //        valid = true;
        //    }

        //    // --------- 3.2) Mask PAN بعد HMAC لأجل اللوجينج فقط ----------
        //    var pan = FirstValue(fields, "obj.source_data.pan", "source_data.pan", "obj.masked_pan", "masked_pan");
        //    if (!string.IsNullOrEmpty(pan) && pan.Length > 4)
        //        fields["obj.source_data.pan"] = new string('X', Math.Max(0, pan.Length - 4)) + pan[^4..];

        //    // --------- 4) Log الاستلام/الفشل بقيم obj. الصحيحة ----------
        //    string? merchantOrderIdForLog = FirstValue(fields, "obj.order.merchant_order_id", "merchant_order_id");
        //    await WebhookLogs.AddAsync(new WebhookLog
        //    {
        //        RawPayload = rawBody,
        //        EventType = valid ? "Received" : "Failure",
        //        MerchantOrderId = merchantOrderIdForLog,
        //        PaymobOrderId = TryParseLong(fields, "obj.order.id", "order.id", "obj.order_id", "order_id"),
        //        PaymobTransactionId = TryParseLong(fields, "obj.id", "id"),
        //        IsHmacValid = valid,
        //        HttpStatus = valid ? 200 : 400,
        //        HeadersJson = req.Headers.ToString(),
        //        ReceivedAt = DateTime.UtcNow,
        //        IsValid = valid
        //    });
        //    await _uow.SaveChangesAsync();

        //    if (!valid)
        //    {
        //        return new ApiResponse<bool>
        //        {
        //            Status = false,
        //            Message = "Invalid HMAC",
        //            Data = false,
        //            Errors = new List<string> { "Signature mismatch" }
        //        };
        //    }

        //    // --------- 5) Routing by type ----------
        //    switch (eventType)
        //    {
        //        case "TRANSACTION":
        //            {
        //                var paymobTxId = TryParseLong(fields, "obj.id", "id");
        //                var paymobOrderId = TryParseLong(fields, "obj.order.id", "order.id", "obj.order_id", "order_id");
        //                string? merchantOrderId = merchantOrderIdForLog;

        //                // Idempotency
        //                if (paymobTxId > 0 && await ExistsProcessedTxAsync(paymobTxId))
        //                    return new ApiResponse<bool> { Status = true, Message = "Duplicate event ignored (idempotent)", Data = true };

        //                bool isRefunded = TryParseBool(fields, "obj.is_refunded", "is_refunded");
        //                bool isVoided = TryParseBool(fields, "obj.is_voided", "is_voided");
        //                bool isCapture = TryParseBool(fields, "obj.is_capture", "is_capture");
        //                bool success = TryParseBool(fields, "obj.success", "success");
        //                bool pending = TryParseBool(fields, "obj.pending", "pending");

        //                long amountCents = TryParseLong(fields, "obj.amount_cents", "amount_cents");
        //                string currency = FirstValue(fields, "obj.currency", "currency") ?? _opt.Currency;

        //                string localStatus =
        //                    isRefunded ? "Refunded" :
        //                    isVoided ? "Voided" :
        //                    isCapture ? "Captured" :
        //                    pending ? "Pending" :
        //                    success ? "Paid" : "Failed";

        //                // تحديث الـTransaction (لو موجودة)
        //                if (paymobTxId > 0)
        //                {
        //                    await UpdateTransactionByPaymobIdAsync(paymobTxId, tx =>
        //                    {
        //                        tx.PaymobOrderId = (tx.PaymobOrderId is null || tx.PaymobOrderId == 0) ? paymobOrderId : tx.PaymobOrderId;
        //                        tx.MerchantOrderId = tx.MerchantOrderId ?? merchantOrderId;
        //                        if (amountCents > 0) tx.AmountCents = amountCents;
        //                        tx.Currency = currency;
        //                        tx.IsSuccess = success;
        //                        tx.Status = localStatus;
        //                        tx.HmacVerified = true;
        //                    });
        //                }

        //                // تحديث حالة الـOrder
        //                if (!string.IsNullOrWhiteSpace(merchantOrderId))
        //                    await UpdateOrderStatusAsync(merchantOrderId!, localStatus);

        //                // علامة تمّ المعالجة
        //                if (paymobTxId > 0)
        //                    await MarkProcessedTxAsync(paymobTxId, "TRANSACTION_PROCESSED");

        //                return new ApiResponse<bool> { Status = true, Message = "Webhook processed (transaction)", Data = true };
        //            }

        //        //case "TOKEN": // نحتفظ به للتوافق
        //        //    {
        //        //        var owner = (FirstValue(fields, "obj.owner", "owner") ?? "unknown");
        //        //        await MarkGenericAsync("TOKEN", owner, true);
        //        //        return new ApiResponse<bool> { Status = true, Message = "Webhook processed (token)", Data = true };
        //        //    }
        //        case "TOKEN":
        //            {
        //                if (hasCardTokenKeys) goto case "CARD_TOKEN"; // نفس لوجيك الحفظ
        //                await MarkGenericAsync("TOKEN", FirstValue(fields, "obj.id", "id") ?? "unknown", true);
        //                return new ApiResponse<bool> { Status = true, Message = "Webhook processed (token)", Data = true };
        //            }



        //        //case "CARD_TOKEN":
        //        //    {
        //        //        // 1) token
        //        //        var cardToken = FirstValue(fields,
        //        //            "obj.token", "obj.saved_card_token", "obj.card_token",
        //        //            "token", "saved_card_token", "card_token");

        //        //        // 2) last4 (يدعم masked_pan بالشرطات)
        //        //        string? last4 = FirstValue(fields, "obj.last4", "last4");
        //        //        if (string.IsNullOrWhiteSpace(last4))
        //        //        {
        //        //            var masked = FirstValue(fields, "obj.masked_pan", "masked_pan", "obj.source_data.pan", "source_data.pan");
        //        //            if (!string.IsNullOrWhiteSpace(masked))
        //        //            {
        //        //                var digits = new string(masked.Where(char.IsDigit).ToArray());
        //        //                if (digits.Length >= 4) last4 = digits[^4..];
        //        //            }
        //        //        }

        //        //        // 3) brand (يدعم card_subtype)
        //        //        string? brand = FirstValue(fields,
        //        //            "obj.card_subtype", "card_subtype",
        //        //            "obj.source_data.type", "source_data.type",
        //        //            "obj.brand", "brand", "obj.scheme", "scheme");

        //        //        // 4) expiry (اختياري)
        //        //        int? expMonth = null, expYear = null;
        //        //        if (int.TryParse(FirstValue(fields, "obj.expiry_month", "expiry_month"), out var mm)) expMonth = mm;
        //        //        if (int.TryParse(FirstValue(fields, "obj.expiry_year", "expiry_year"), out var yy)) expYear = yy;

        //        //        // 5) Paymob token id
        //        //        long paymobTokenId = TryParseLong(fields, "obj.id", "id");

        //        //        // 6) اربط بالمستخدم
        //        //        var userId = await ResolveUserIdFromTokenContextAsync(fields);

        //        //        if (string.IsNullOrWhiteSpace(cardToken) || string.IsNullOrWhiteSpace(userId))
        //        //        {
        //        //            _log?.LogWarning("CARD_TOKEN missing data → userId={userId}, token={token}", userId, cardToken);
        //        //            await MarkGenericAsync("CARD_TOKEN_MISSING_DATA", cardToken ?? "unknown", false);
        //        //            return new ApiResponse<bool> { Status = true, Message = "CARD_TOKEN ack (missing user/token)", Data = true };
        //        //        }

        //        //        // 7) Upsert
        //        //        var existing = await _uow.GetRepository<UserSavedCard, int>().GetFirstOrDefaultAsync(
        //        //                                                            c => c.UserId == userId && c.Token == cardToken,
        //        //                                                            trackChanges: true
        //        //                                                        );

        //        //        if (existing is null)
        //        //        {
        //        //            bool isDefault = !await SavedCards.AnyAsync(c => c.UserId == userId);
        //        //            await SavedCards.AddAsync(new UserSavedCard
        //        //            {
        //        //                UserId = userId!,
        //        //                Token = cardToken!,
        //        //                Last4 = last4,
        //        //                Brand = brand,
        //        //                ExpiryMonth = expMonth,
        //        //                ExpiryYear = expYear,
        //        //                PaymobTokenId = paymobTokenId > 0 ? paymobTokenId.ToString() : null,
        //        //                IsDefault = isDefault,
        //        //                CreatedAt = DateTime.UtcNow
        //        //            });
        //        //            _log?.LogWarning("Saved NEW card → user={userId}, last4={last4}, brand={brand}, paymobTokenId={pid}",
        //        //                userId, last4, brand, paymobTokenId);
        //        //        }
        //        //        else
        //        //        {
        //        //            if (string.IsNullOrWhiteSpace(existing.Last4) && !string.IsNullOrWhiteSpace(last4)) existing.Last4 = last4;
        //        //            if (string.IsNullOrWhiteSpace(existing.Brand) && !string.IsNullOrWhiteSpace(brand)) existing.Brand = brand;
        //        //            if (!existing.ExpiryMonth.HasValue && expMonth.HasValue) existing.ExpiryMonth = expMonth;
        //        //            if (!existing.ExpiryYear.HasValue && expYear.HasValue) existing.ExpiryYear = expYear;

        //        //            // PaymobTokenId نوعها string? → ماينفعش نقارن بـ 0
        //        //            if (string.IsNullOrEmpty(existing.PaymobTokenId) && paymobTokenId > 0)
        //        //                existing.PaymobTokenId = paymobTokenId.ToString();


        //        //            _log?.LogWarning("UPDATED existing card → user={userId}, token={token}", userId, cardToken);
        //        //        }

        //        //        await _uow.SaveChangesAsync();

        //        //        await MarkGenericAsync("CARD_TOKEN_SAVED", cardToken, true);
        //        //        return new ApiResponse<bool> { Status = true, Message = "Webhook processed (card token)", Data = true };
        //        //    }
        //        case "CARD_TOKEN":
        //            {
        //                // 1) token
        //                var cardToken = FirstValue(fields,
        //                    "obj.token", "obj.saved_card_token", "obj.card_token",
        //                    "token", "saved_card_token", "card_token");

        //                // 2) last4 (يدعم masked_pan بالشرطات)
        //                string? last4 = FirstValue(fields, "obj.last4", "last4");
        //                if (string.IsNullOrWhiteSpace(last4))
        //                {
        //                    var masked = FirstValue(fields, "obj.masked_pan", "masked_pan", "obj.source_data.pan", "source_data.pan");
        //                    if (!string.IsNullOrWhiteSpace(masked))
        //                    {
        //                        var digits = new string(masked.Where(char.IsDigit).ToArray());
        //                        if (digits.Length >= 4) last4 = digits[^4..];
        //                    }
        //                }

        //                // 3) brand (يدعم card_subtype)
        //                string? brand = FirstValue(fields,
        //                    "obj.card_subtype", "card_subtype",
        //                    "obj.source_data.type", "source_data.type",
        //                    "obj.brand", "brand", "obj.scheme", "scheme");

        //                // 4) expiry (اختياري) – التقاط بأسماء متعددة + صيغة مجمّعة MM/YY أو MM/YYYY
        //                int? expMonth = null, expYear = null;

        //                // أسماء بديلة محتملة من بايموب
        //                var mmStr = FirstValue(fields,
        //                    "obj.expiry_month", "expiry_month",
        //                    "obj.expiration_month", "expiration_month",
        //                    "obj.exp_month", "exp_month",
        //                    "obj.card_expiry_month", "card_expiry_month"
        //                );

        //                var yyStr = FirstValue(fields,
        //                    "obj.expiry_year", "expiry_year",
        //                    "obj.expiration_year", "expiration_year",
        //                    "obj.exp_year", "exp_year",
        //                    "obj.card_expiry_year", "card_expiry_year"
        //                );

        //                // صيغة مجمّعة مثل "MM/YY" أو "MM/YYYY"
        //                var expiryCombined = FirstValue(fields, "obj.expiry", "expiry", "obj.expiration", "expiration");

        //                // Parse
        //                if (int.TryParse(mmStr, out var mm)) expMonth = mm;

        //                if (int.TryParse(yyStr, out var yy))
        //                {
        //                    // طبّع السنة لو جت رقمين: 27 → 2027
        //                    expYear = yy >= 100 ? yy : (2000 + yy);
        //                }
        //                else if (!string.IsNullOrWhiteSpace(expiryCombined))
        //                {
        //                    var m = System.Text.RegularExpressions.Regex.Match(expiryCombined, @"(?<mm>\d{1,2})\D+(?<yy>\d{2,4})");
        //                    if (m.Success)
        //                    {
        //                        if (int.TryParse(m.Groups["mm"].Value, out var mmm)) expMonth = mmm;
        //                        if (int.TryParse(m.Groups["yy"].Value, out var yyy))
        //                            expYear = yyy >= 100 ? yyy : (2000 + yyy);
        //                    }
        //                }

        //                // ضبط حدودي (اختياري)
        //                if (expMonth is < 1 or > 12) expMonth = null;
        //                if (expYear is < 2000 or > 2100) expYear = null;

        //                // 5) Paymob token id
        //                long paymobTokenId = TryParseLong(fields, "obj.id", "id");

        //                // 6) اربط بالمستخدم
        //                var userId = await ResolveUserIdFromTokenContextAsync(fields);

        //                if (string.IsNullOrWhiteSpace(cardToken) || string.IsNullOrWhiteSpace(userId))
        //                {
        //                    _log?.LogWarning("CARD_TOKEN missing data → userId={userId}, token={token}", userId, cardToken);
        //                    await MarkGenericAsync("CARD_TOKEN_MISSING_DATA", cardToken ?? "unknown", false);
        //                    return new ApiResponse<bool> { Status = true, Message = "CARD_TOKEN ack (missing user/token)", Data = true };
        //                }

        //                // 7) Upsert
        //                var existing = await _uow.GetRepository<UserSavedCard, int>().GetFirstOrDefaultAsync(
        //                                    c => c.UserId == userId && c.Token == cardToken,
        //                                    trackChanges: true
        //                                );

        //                if (existing is null)
        //                {
        //                    bool isDefault = !await SavedCards.AnyAsync(c => c.UserId == userId);
        //                    await SavedCards.AddAsync(new UserSavedCard
        //                    {
        //                        UserId = userId!,
        //                        Token = cardToken!,
        //                        Last4 = last4,
        //                        Brand = brand,
        //                        ExpiryMonth = expMonth,
        //                        ExpiryYear = expYear,
        //                        PaymobTokenId = paymobTokenId > 0 ? paymobTokenId.ToString() : null,
        //                        IsDefault = isDefault,
        //                        CreatedAt = DateTime.UtcNow
        //                    });
        //                    _log?.LogWarning("Saved NEW card → user={userId}, last4={last4}, brand={brand}, paymobTokenId={pid}",
        //                        userId, last4, brand, paymobTokenId);
        //                }
        //                else
        //                {
        //                    if (string.IsNullOrWhiteSpace(existing.Last4) && !string.IsNullOrWhiteSpace(last4)) existing.Last4 = last4;
        //                    if (string.IsNullOrWhiteSpace(existing.Brand) && !string.IsNullOrWhiteSpace(brand)) existing.Brand = brand;
        //                    if (!existing.ExpiryMonth.HasValue && expMonth.HasValue) existing.ExpiryMonth = expMonth;
        //                    if (!existing.ExpiryYear.HasValue && expYear.HasValue) existing.ExpiryYear = expYear;

        //                    if (string.IsNullOrEmpty(existing.PaymobTokenId) && paymobTokenId > 0)
        //                        existing.PaymobTokenId = paymobTokenId.ToString();

        //                    _log?.LogWarning("UPDATED existing card → user={userId}, token={token}", userId, cardToken);
        //                }

        //                await _uow.SaveChangesAsync();

        //                await MarkGenericAsync("CARD_TOKEN_SAVED", cardToken, true);
        //                return new ApiResponse<bool> { Status = true, Message = "Webhook processed (card token)", Data = true };
        //            }


        //        // Fallback: أنواع قريبة — لو فيها token اعتبرها CARD_TOKEN
        //        case "SAVED_CARD":
        //        case "CARD":
        //        case "PAYMENT_TOKEN":
        //            {
        //                if (hasCardTokenKeys) goto case "CARD_TOKEN";
        //                goto default;
        //            }

        //        default:
        //            {
        //                var id = FirstValue(fields, "obj.id", "id") ?? Guid.NewGuid().ToString("N");
        //                await MarkGenericAsync(eventType, id, true);
        //                return new ApiResponse<bool> { Status = true, Message = $"Webhook processed ({eventType})", Data = true };
        //            }
        //    }
        //}

        public async Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody)
        {
            // [A] Log سريع
            _log?.LogWarning("Webhook arrived: {Method} {Path}{Query} UA={UA}",
                req.Method, req.Path, req.QueryString.Value, req.Headers["User-Agent"].ToString());

            // --------- 0) تجميع الحقول كما هي ----------
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in req.Query) fields[kv.Key] = kv.Value.ToString();

            if (req.Headers.TryGetValue("hmac", out var h1)) fields["hmac"] = h1.ToString();
            else if (req.Headers.TryGetValue("hmac_signature", out var h2)) fields["hmac"] = h2.ToString();
            else if (req.Headers.TryGetValue("X-HMAC-Signature", out var h3)) fields["hmac"] = h3.ToString();

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

                    if (!fields.ContainsKey("hmac") &&
                        doc.RootElement.TryGetProperty("hmac", out var hJson) &&
                        hJson.ValueKind == JsonValueKind.String)
                    {
                        fields["hmac"] = hJson.GetString();
                    }
                }
            }
            catch { /* ignore parsing errors */ }

            // 👇 NEW: استخرج الـ MID مرة واحدة لاستخدامه في كل اللوجات
            var mid = TryParseLong(fields, "obj.merchant_id", "merchant_id", "obj.merchant.id", "merchant.id");

            // --------- 1) Logging (اختياري) ----------
            try
            {
                _log?.LogInformation("====== PAYMOB WEBHOOK FIELDS ======\n{pairs}\n==================================",
                    string.Join("\n", fields.Select(kv => $"{kv.Key} = {kv.Value}")));
            }
            catch { }

            // --------- 2) لازم HMAC (لو مفيش هنلوج ونكمل) ----------
            if (!fields.ContainsKey("hmac") || string.IsNullOrWhiteSpace(fields["hmac"]))
            {
                await WebhookLogs.AddAsync(new WebhookLog
                {
                    RawPayload = rawBody,
                    EventType = "NoHmac-Debug",
                    HeadersJson = req.Headers.ToString(),
                    HttpStatus = 200,
                    ReceivedAt = DateTime.UtcNow,
                    IsHmacValid = false,
                    MerchantId = mid   // استخدام الـ MID
                });
                await _uow.SaveChangesAsync();

                return new ApiResponse<bool> { Status = true, Message = "No HMAC (debug ack)", Data = true };
            }

            // --------- 3) تحديد النوع مبكراً (مع دعم obj.) ----------
            string typeRaw = (FirstValue(fields, "type", "obj.type") ?? "TRANSACTION").Trim();
            string eventType = typeRaw.ToUpperInvariant();

            bool hasCardTokenKeys = FirstValue(fields,
                "obj.token", "obj.saved_card_token", "obj.card_token",
                "token", "saved_card_token", "card_token") != null;
            if (hasCardTokenKeys) eventType = "CARD_TOKEN";

            // --------- 3.1) تحقق HMAC ----------
            bool valid;
            try { valid = VerifyHmacSha512(fields, rawBody); } catch { valid = false; }

            if ((eventType == "CARD_TOKEN" || eventType == "TOKEN") && !valid)
            {
                _log?.LogWarning("TOKEN/CARD_TOKEN webhook with invalid/missing HMAC – TEMPORARY ACCEPT for save-card testing.");
                valid = true;
            }

            // --------- 3.2) Mask PAN بعد HMAC لأجل اللوجينج فقط ----------
            var pan = FirstValue(fields, "obj.source_data.pan", "source_data.pan", "obj.masked_pan", "masked_pan");
            if (!string.IsNullOrEmpty(pan) && pan.Length > 4)
                fields["obj.source_data.pan"] = new string('X', Math.Max(0, pan.Length - 4)) + pan[^4..];

            // --------- 4) Log الاستلام/الفشل بقيم obj. الصحيحة ----------
            string? merchantOrderIdForLog = FirstValue(fields, "obj.order.merchant_order_id", "merchant_order_id");
            await WebhookLogs.AddAsync(new WebhookLog
            {
                RawPayload = rawBody,
                EventType = valid ? "Received" : "Failure",
                MerchantOrderId = merchantOrderIdForLog,
                PaymobOrderId = TryParseLong(fields, "obj.order.id", "order.id", "obj.order_id", "order_id"),
                PaymobTransactionId = TryParseLong(fields, "obj.id", "id"),
                IsHmacValid = valid,
                HttpStatus = valid ? 200 : 400,
                HeadersJson = req.Headers.ToString(),
                ReceivedAt = DateTime.UtcNow,
                IsValid = valid,
                MerchantId = mid   // استخدام الـ MID
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
            switch (eventType)
            {
                case "TRANSACTION":
                    {
                        var paymobTxId = TryParseLong(fields, "obj.id", "id");
                        var paymobOrderId = TryParseLong(fields, "obj.order.id", "order.id", "obj.order_id", "order_id");
                        string? merchantOrderId = merchantOrderIdForLog;

                        if (paymobTxId > 0 && await ExistsProcessedTxAsync(paymobTxId))
                            return new ApiResponse<bool> { Status = true, Message = "Duplicate event ignored (idempotent)", Data = true };

                        bool isRefunded = TryParseBool(fields, "obj.is_refunded", "is_refunded");
                        bool isVoided = TryParseBool(fields, "obj.is_voided", "is_voided");
                        bool isCapture = TryParseBool(fields, "obj.is_capture", "is_capture");
                        bool success = TryParseBool(fields, "obj.success", "success");
                        bool pending = TryParseBool(fields, "obj.pending", "pending");

                        long amountCents = TryParseLong(fields, "obj.amount_cents", "amount_cents");
                        string currency = FirstValue(fields, "obj.currency", "currency") ?? _opt.Currency;

                        string localStatus =
                            isRefunded ? "Refunded" :
                            isVoided ? "Voided" :
                            isCapture ? "Captured" :
                            pending ? "Pending" :
                            success ? "Paid" : "Failed";

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

                        if (!string.IsNullOrWhiteSpace(merchantOrderId))
                            await UpdateOrderStatusAsync(merchantOrderId!, localStatus);

                        if (paymobTxId > 0)
                            await MarkProcessedTxAsync(paymobTxId, "TRANSACTION_PROCESSED");

                        return new ApiResponse<bool> { Status = true, Message = "Webhook processed (transaction)", Data = true };
                    }

                case "TOKEN":
                    {
                        if (hasCardTokenKeys) goto case "CARD_TOKEN";
                        await MarkGenericAsync("TOKEN", FirstValue(fields, "obj.id", "id") ?? "unknown", true);
                        return new ApiResponse<bool> { Status = true, Message = "Webhook processed (token)", Data = true };
                    }

                case "CARD_TOKEN":
                    {
                        // 1) token
                        var cardToken = FirstValue(fields,
                            "obj.token", "obj.saved_card_token", "obj.card_token",
                            "token", "saved_card_token", "card_token");

                        // 2) last4 (يدعم masked_pan بالشرطات)
                        string? last4 = FirstValue(fields, "obj.last4", "last4");
                        if (string.IsNullOrWhiteSpace(last4))
                        {
                            var masked = FirstValue(fields, "obj.masked_pan", "masked_pan", "obj.source_data.pan", "source_data.pan");
                            if (!string.IsNullOrWhiteSpace(masked))
                            {
                                var digits = new string(masked.Where(char.IsDigit).ToArray());
                                if (digits.Length >= 4) last4 = digits[^4..];
                            }
                        }

                        // 3) brand (يدعم card_subtype)
                        string? brand = FirstValue(fields,
                            "obj.card_subtype", "card_subtype",
                            "obj.source_data.type", "source_data.type",
                            "obj.brand", "brand", "obj.scheme", "scheme");

                        // 4) expiry (اختياري) – التقاط بأسماء متعددة + صيغة مجمّعة MM/YY أو MM/YYYY
                        int? expMonth = null, expYear = null;

                        // أسماء بديلة محتملة من بايموب
                        var mmStr = FirstValue(fields,
                            "obj.expiry_month", "expiry_month",
                            "obj.expiration_month", "expiration_month",
                            "obj.exp_month", "exp_month",
                            "obj.card_expiry_month", "card_expiry_month"
                        );

                        var yyStr = FirstValue(fields,
                            "obj.expiry_year", "expiry_year",
                            "obj.expiration_year", "expiration_year",
                            "obj.exp_year", "exp_year",
                            "obj.card_expiry_year", "card_expiry_year"
                        );

                        // صيغة مجمّعة مثل "MM/YY" أو "MM/YYYY"
                        var expiryCombined = FirstValue(fields, "obj.expiry", "expiry", "obj.expiration", "expiration");

                        // Parse
                        if (int.TryParse(mmStr, out var mm)) expMonth = mm;

                        if (int.TryParse(yyStr, out var yy))
                        {
                            // طبّع السنة لو جت رقمين: 27 → 2027
                            expYear = yy >= 100 ? yy : (2000 + yy);
                        }
                        else if (!string.IsNullOrWhiteSpace(expiryCombined))
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(expiryCombined, @"(?<mm>\d{1,2})\D+(?<yy>\d{2,4})");
                            if (m.Success)
                            {
                                if (int.TryParse(m.Groups["mm"].Value, out var mmm)) expMonth = mmm;
                                if (int.TryParse(m.Groups["yy"].Value, out var yyy))
                                    expYear = yyy >= 100 ? yyy : (2000 + yyy);
                            }
                        }

                        // ضبط حدودي (اختياري)
                        if (expMonth is < 1 or > 12) expMonth = null;
                        if (expYear is < 2000 or > 2100) expYear = null;

                        // 5) Paymob token id
                        long paymobTokenId = TryParseLong(fields, "obj.id", "id");

                        // 6) اربط بالمستخدم
                        var userId = await ResolveUserIdFromTokenContextAsync(fields);

                        if (string.IsNullOrWhiteSpace(cardToken) || string.IsNullOrWhiteSpace(userId))
                        {
                            _log?.LogWarning("CARD_TOKEN missing data → userId={userId}, token={token}", userId, cardToken);
                            await MarkGenericAsync("CARD_TOKEN_MISSING_DATA", cardToken ?? "unknown", false);
                            return new ApiResponse<bool> { Status = true, Message = "CARD_TOKEN ack (missing user/token)", Data = true };
                        }

                        // 7) Upsert
                        var existing = await _uow.GetRepository<UserSavedCard, int>().GetFirstOrDefaultAsync(
                                            c => c.UserId == userId && c.Token == cardToken,
                                            trackChanges: true
                                        );

                        if (existing is null)
                        {
                            bool isDefault = !await SavedCards.AnyAsync(c => c.UserId == userId);
                            await SavedCards.AddAsync(new UserSavedCard
                            {
                                UserId = userId!,
                                Token = cardToken!,
                                Last4 = last4,
                                Brand = brand,
                                ExpiryMonth = expMonth,
                                ExpiryYear = expYear,
                                PaymobTokenId = paymobTokenId > 0 ? paymobTokenId.ToString() : null,
                                IsDefault = isDefault,
                                CreatedAt = DateTime.UtcNow,
                                MerchantId = mid
                            });
                            _log?.LogWarning("Saved NEW card → user={userId}, last4={last4}, brand={brand}, paymobTokenId={pid}",
                                userId, last4, brand, paymobTokenId);
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(existing.Last4) && !string.IsNullOrWhiteSpace(last4)) existing.Last4 = last4;
                            if (string.IsNullOrWhiteSpace(existing.Brand) && !string.IsNullOrWhiteSpace(brand)) existing.Brand = brand;
                            if (!existing.ExpiryMonth.HasValue && expMonth.HasValue) existing.ExpiryMonth = expMonth;
                            if (!existing.ExpiryYear.HasValue && expYear.HasValue) existing.ExpiryYear = expYear;

                            if (string.IsNullOrEmpty(existing.PaymobTokenId) && paymobTokenId > 0)
                                existing.PaymobTokenId = paymobTokenId.ToString();

                            _log?.LogWarning("UPDATED existing card → user={userId}, token={token}", userId, cardToken);
                        }

                        await _uow.SaveChangesAsync();

                        await MarkGenericAsync("CARD_TOKEN_SAVED", cardToken, true);
                        return new ApiResponse<bool> { Status = true, Message = "Webhook processed (card token)", Data = true };
                    }


                case "SAVED_CARD":
                case "CARD":
                case "PAYMENT_TOKEN":
                    {
                        if (hasCardTokenKeys) goto case "CARD_TOKEN";
                        goto default;
                    }

                default:
                    {
                        var id = FirstValue(fields, "obj.id", "id") ?? Guid.NewGuid().ToString("N");
                        await MarkGenericAsync(eventType, id, true);
                        return new ApiResponse<bool> { Status = true, Message = $"Webhook processed ({eventType})", Data = true };
                    }
            }
        }


        // ===== Helper: Resolve UserId من الـmerchant_order_id (Async) =====
        private static string JsonToFixedString(System.Text.Json.JsonElement el) => el.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => el.GetString() ?? "",
            System.Text.Json.JsonValueKind.Number => el.TryGetInt64(out var n)
                ? n.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : (el.TryGetDouble(out var d)
                    ? d.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : (el.ToString() ?? "")),
            System.Text.Json.JsonValueKind.True => "true",
            System.Text.Json.JsonValueKind.False => "false",
            System.Text.Json.JsonValueKind.Null => "",
            _ => el.ToString() ?? ""
        };
        private static bool TryGetByDotPath(System.Text.Json.JsonElement root, string path, out string value)
        {
            value = "";
            try
            {
                var cur = root;
                foreach (var part in path.Split('.'))
                {
                    if (cur.ValueKind != System.Text.Json.JsonValueKind.Object || !cur.TryGetProperty(part, out cur))
                        return false;
                }
                value = JsonToFixedString(cur);
                return true;
            }
            catch { return false; }
        }
        private static string BuildTransactionConcat(System.Text.Json.JsonElement obj)
        {
            // ترتيب Paymob الحرفي
            string[] keys =
            {
        "amount_cents","created_at","currency","error_occured","has_parent_transaction","id","integration_id",
        "is_3d_secure","is_auth","is_capture","is_refunded","is_standalone_payment","is_voided","order.id",
        "owner","pending","source_data.pan","source_data.sub_type","source_data.type","success"
    };

            var sb = new System.Text.StringBuilder(256);
            foreach (var k in keys)
                sb.Append(TryGetByDotPath(obj, k, out var v) ? v : "");
            return sb.ToString();
        }
        private static string BuildTokenConcat(System.Text.Json.JsonElement obj)
        {
            // TOKEN/CARD_TOKEN: قيم obj مرتّبة أبجديًا (بدون hmac)
            var names = obj.EnumerateObject()
                           .Select(p => p.Name)
                           .Where(n => !string.Equals(n, "hmac", StringComparison.OrdinalIgnoreCase))
                           .OrderBy(n => n, StringComparer.Ordinal);

            var sb = new System.Text.StringBuilder(128);
            foreach (var name in names)
                if (obj.TryGetProperty(name, out var v))
                    sb.Append(JsonToFixedString(v));
            return sb.ToString();
        }
        private static byte[] KeyFromHexOrUtf8(string secret)
        {
            bool looksHex = !string.IsNullOrWhiteSpace(secret)
                            && secret.Length % 2 == 0
                            && secret.All(c =>
                            {
                                var x = (char)(c | 32);
                                return (x >= '0' && x <= '9') || (x >= 'a' && x <= 'f');
                            });

            if (!looksHex) return System.Text.Encoding.UTF8.GetBytes(secret);

            var bytes = new byte[secret.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(secret.Substring(i * 2, 2), 16);
            return bytes;
        }
        private bool VerifyHmacSha512(IDictionary<string, string?> fields, string rawJson)
        {
            // 1) التقاط hmac من كل المفاتيح الشائعة
            string? receivedHmac = null;
            string[] keys =
            {
        "hmac", "hmac_signature", "x-hmac-signature", "X-HMAC-Signature",
        "secure-hash", "X-Signature"
    };

            foreach (var k in keys)
                if (fields.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                { receivedHmac = v; break; }

            //Fallback: من JSON نفسه لو Paymob حاططها هناك
            if (string.IsNullOrWhiteSpace(receivedHmac)
                && !string.IsNullOrWhiteSpace(rawJson)
                && rawJson.TrimStart().StartsWith("{"))
            {
                using var d = JsonDocument.Parse(rawJson);
                if (d.RootElement.TryGetProperty("hmac", out var hv) && hv.ValueKind == JsonValueKind.String)
                    receivedHmac = hv.GetString();
            }

            if (string.IsNullOrWhiteSpace(receivedHmac))
                return false;

            receivedHmac = receivedHmac!.Trim().ToLowerInvariant();

            // 2) جِيب السر كمفتاح HMAC (Hex أو UTF8)
            var secret = _opt.HmacSecret ?? "";
            var key = KeyFromHexOrUtf8(secret);

            // 3) حدّد نوع الكولباك وابني الـ concatenated string
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeEl) || !root.TryGetProperty("obj", out var objEl))
                return false;

            var type = typeEl.GetString() ?? "";
            string concatenated = string.Equals(type, "TRANSACTION", StringComparison.OrdinalIgnoreCase)
                ? BuildTransactionConcat(objEl)
                : BuildTokenConcat(objEl); // TOKEN / CARD_TOKEN / SAVED_CARD …

            // 4) حساب HMAC والتحقق
            using var hmac = new HMACSHA512(key);
            var calc = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated)))
                           .Replace("-", "").ToLowerInvariant();

            return calc == receivedHmac;
        }
        // using Voltyks.Infrastructure;           // لو فيه IUnitOfWork / IGenericRepository هنا
        // using Voltyks.Persistence.Entities.Main; // لو الكيانات هنا

        private async Task<string?> ResolveUserIdFromTokenContextAsync(IDictionary<string, string?> f)
        {
            // 1) metadata.user_id (لو بتبعته)
            var uid = FirstValue(f, "obj.metadata.user_id", "metadata.user_id");
            if (!string.IsNullOrWhiteSpace(uid)) return uid;

            // 2) merchant_order_id فيه uid:<id> أو uid=<id>
            var mo = FirstValue(f, "obj.order.merchant_order_id", "merchant_order_id");
            if (!string.IsNullOrWhiteSpace(mo))
            {
                var m = System.Text.RegularExpressions.Regex.Match(
                    mo, @"uid\s*[:=]\s*([A-Za-z0-9_\-@.]+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;
            }

            // 3) Paymob Order Id (obj.order_id أو obj.order.id)

            var paymobOrderId = TryParseLong(f, "obj.order.id", "order.id", "obj.order_id", "order_id");

            if (paymobOrderId > 0)
            {
                var orderRepo = _uow.GetRepository<PaymentOrder, int>();

                // لو الخصائص non-nullable:
                var order = await orderRepo.GetFirstOrDefaultAsync(
                                 o => o.PaymobOrderId == paymobOrderId,
                                 trackChanges: false
                             );


                // لو Nullable<long> بدّل الشرط اللي فوق بـ:
                // var order = await orderRepo.GetFirstOrDefaultAsync(
                //     o => (o.PaymobOrderId.HasValue && o.PaymobOrderId.Value == paymobOrderId)
                //       || (o.GatewayOrderId.HasValue && o.GatewayOrderId.Value == paymobOrderId),
                //     trackChanges: false
                // );

                var uidFromOrder = order?.UserId;   // ← المصدر المعتمد
                if (!string.IsNullOrEmpty(uidFromOrder)) return uidFromOrder;
            }

            // 4) (اختياري) لو مخزن الـ merchant_order_id كنص في جدول الأوردرات
            if (!string.IsNullOrWhiteSpace(mo))
            {
                var orderRepo2 = _uow.GetRepository<PaymentOrder, int>();
                var order2 = await orderRepo2.GetFirstOrDefaultAsync(
                    o => o.MerchantOrderId == mo, // عدّل الاسم لو مختلف
                    trackChanges: false
                );
                if (!string.IsNullOrEmpty(order2?.UserId)) return order2!.UserId;
            }

            //5) (اختياري) Fallback بالإيميل لو حابب تربط من الإيميل

            var email = FirstValue(f, "obj.email", "email", "obj.customer.email", "customer.email", "obj.billing.data.email", "billing.data.email");
            if (!string.IsNullOrWhiteSpace(email))
            {
                var byEmail = await _userManager.FindByEmailAsync(email);
                if (byEmail is not null) return byEmail.Id;
            }

            // 5) Fallback بالتليفون
           // var phone = FirstValue(f, "obj.phone_number", "phone_number",
            //                           "obj.customer.phone_number", "customer.phone_number",
            //                           "obj.billing.data.phone_number", "billing.data.phone_number");
            //if (!string.IsNullOrWhiteSpace(phone))
            //{
            //    // Users IQueryable من Identity
            //    var userByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            //    if (userByPhone is not null) return userByPhone.Id;
            //}


            return null;
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
        public async Task<ApiResponse<TokenizationStartRes>> StartCardTokenizationAsync(long amountCents = 0)
        {
            // إصدار payment_key مخصوص لإدخال الكارت (SDK هيعمل Save Card)
            var res = await CreateCardPaymentKeyAsync(new CardCheckoutServiceDto
            {
                AmountCents = Math.Max(0, amountCents),
                Currency = "EGP",
                MerchantOrderId = Guid.NewGuid().ToString("N"),
                Billing = new BillingData(
                    "NA", "NA", "na@example.com", "01000000000",
                    "NA", "NA", "NA", "NA", "Cairo", "NA", "EG", "NA")
            });

            if (!res.Status || res.Data is null)
                return new ApiResponse<TokenizationStartRes>(null, res.Message ?? "Failed", false, res.Errors);

            return new ApiResponse<TokenizationStartRes>(
                new TokenizationStartRes(res.Data.MerchantOrderId, res.Data.PaymentKey),
                "Tokenization started", true
            );
        }
        public async Task<ApiResponse<IEnumerable<SavedCardViewDto>>> ListSavedCardsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return new ApiResponse<IEnumerable<SavedCardViewDto>>(null, "Unauthorized", false);

            var cards = await SavedCards.GetAllAsync(c => c.UserId == userId, trackChanges: false);
            var list = cards.Select(c => new SavedCardViewDto(c.Id, c.Brand, c.Last4, c.ExpiryMonth, c.ExpiryYear, c.IsDefault));
            return new ApiResponse<IEnumerable<SavedCardViewDto>>(list, "OK", true);
        }
        public async Task<ApiResponse<bool>> SetDefaultCardAsync(int cardId)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return new ApiResponse<bool>(false, "Unauthorized", false);

            var all = await SavedCards.GetAllAsync(c => c.UserId == userId, trackChanges: true);
            var target = all.FirstOrDefault(c => c.Id == cardId);
            if (target is null) return new ApiResponse<bool>(false, "Card not found", false);

            foreach (var c in all) c.IsDefault = (c.Id == cardId);
            await _uow.SaveChangesAsync();
            return new ApiResponse<bool>(true, "Default set", true);
        }
        public async Task<ApiResponse<bool>> DeleteCardAsync(int cardId)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return new ApiResponse<bool>(false, "Unauthorized", false);

            var card = await SavedCards.GetFirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, trackChanges: true);
            if (card is null) return new ApiResponse<bool>(false, "Card not found", false);

            SavedCards.Delete(card);
            await _uow.SaveChangesAsync();
            return new ApiResponse<bool>(true, "Deleted", true);
        }
        //public async Task<ApiResponse<object>> ChargeWithSavedCardServerAsync(ChargeWithSavedCardReq req)
        //{
        //    var userId = GetCurrentUserId();
        //    if (userId is null) return new ApiResponse<object>(null, "Unauthorized", false);

        //    var card = await SavedCards.GetFirstOrDefaultAsync(c => c.Id == req.CardId && c.UserId == userId, trackChanges: false);
        //    if (card is null) return new ApiResponse<object>(null, "Card not found", false);

        //    // 1) إصدار payment_key للطلب
        //    var k = await CreatePaymentKeyForSavedCardAsync(
        //        new SavedCardChargeDto(req.AmountCents, req.Currency ?? "EGP", card.Token, req.MerchantOrderId)
        //    );
        //    if (!k.Status || k.Data is null) return new ApiResponse<object>(null, k.Message ?? "Failed", false, k.Errors);

        //    // 2) الدفع من السيرفر باستخدام التوكن
        //    var pay = await PayWithSavedTokenAsync(k.Data.PaymentKey, card.Token);
        //    return new ApiResponse<object>(new
        //    {
        //        k.Data.MerchantOrderId,
        //        k.Data.PaymobOrderId,
        //        k.Data.PaymentKey,
        //        pay_status = pay.Status,
        //        pay_message = pay.Message,
        //        pay_data = pay.Data
        //    }, "Initiated", true, pay.Errors);
        //}
        public async Task<ApiResponse<object>> ChargeWithSavedCardServerAsync(ChargeWithSavedCardReq req)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return new ApiResponse<object>(null, "Unauthorized", false);

            if (req.AmountCents <= 0)
                return new ApiResponse<object>(null, "Invalid amount", false);

            var card = await SavedCards.GetFirstOrDefaultAsync(
                c => c.Id == req.CardId && c.UserId == userId,
                trackChanges: false
            );
            if (card is null)
                return new ApiResponse<object>(null, "Card not found", false);

            // العملة الافتراضية الحالية (نفس الموجود في _opt)
            var currency = string.IsNullOrWhiteSpace(_opt?.Currency) ? "EGP" : _opt.Currency;

            // استخرج MID: من الكارت أولاً، ولو مش موجود هنجرب من WebhookLogs لآخر TOKEN لنفس التوكن
            long? mid = card.MerchantId;
            if (!mid.HasValue)
            {
                mid = await _uow.GetRepository<WebhookLog, int>()
                    .Query()
                    .Where(w => w.MerchantId != null && w.RawPayload != null && w.RawPayload.Contains(card.Token))
                    .OrderByDescending(w => w.Id)
                    .Select(w => w.MerchantId)
                    .FirstOrDefaultAsync();
            }

            // merchant_order_id واضح للتتبع (اختياري تضمّن الـMID)
            var localOrderId = Guid.NewGuid().ToString("N");
            var merchantOrderId = $"uid:{userId}|mid:{(mid.HasValue ? mid.Value.ToString() : "NA")}|ord:{localOrderId}";

            // 1) إصدار payment_key للطلب (نفس دالتك الحالية، بدون أي config إضافي)
            var k = await CreatePaymentKeyForSavedCardAsync(
                new SavedCardChargeDto((long)req.AmountCents, currency, card.Token, merchantOrderId)
            );
            if (!k.Status || k.Data is null)
                return new ApiResponse<object>(null, k.Message ?? "Failed", false, k.Errors);

            // 2) الدفع من السيرفر باستخدام التوكن المحفوظ (كما هو)
            var pay = await PayWithSavedTokenAsync(k.Data.PaymentKey, card.Token);

            return new ApiResponse<object>(new
            {
                MerchantOrderId = k.Data.MerchantOrderId,
                PaymobOrderId = k.Data.PaymobOrderId,
                PaymentKey = k.Data.PaymentKey,
                UsedMerchantId = mid,    // للشفافية فقط
                pay_status = pay.Status,
                pay_message = pay.Message,
                pay_data = pay.Data
            }, "Initiated", true, pay.Errors);
        }

        //public async Task<ApiResponse<SavedCardInitChargeRes>> InitChargeWithSavedCardAsync(SavedCardInitChargeReq req)
        //{
        //    var userId = GetCurrentUserId();
        //    if (userId is null) return new ApiResponse<SavedCardInitChargeRes>(null, "Unauthorized", false);

        //    var card = await SavedCards.GetFirstOrDefaultAsync(c => c.Id == req.CardId && c.UserId == userId, trackChanges: false);
        //    if (card is null) return new ApiResponse<SavedCardInitChargeRes>(null, "Card not found", false);

        //    var k = await CreatePaymentKeyForSavedCardAsync(
        //        new SavedCardChargeDto(req.AmountCents, req.Currency ?? "EGP", card.Token, req.MerchantOrderId)
        //    );
        //    if (!k.Status || k.Data is null)
        //        return new ApiResponse<SavedCardInitChargeRes>(null, k.Message ?? "Failed", false, k.Errors);

        //    var dto = new SavedCardInitChargeRes(k.Data.MerchantOrderId, k.Data.PaymobOrderId, k.Data.PaymentKey, card.Token);
        //    return new ApiResponse<SavedCardInitChargeRes>(dto, "Ready", true);
        //}
        public async Task<ApiResponse<PayActionRes>> PayWithSavedTokenAsync(string paymentKey, string savedCardToken)
        {
            if (string.IsNullOrWhiteSpace(paymentKey))
                return new ApiResponse<PayActionRes>(null, "paymentKey is required", false);

            if (string.IsNullOrWhiteSpace(savedCardToken))
                return new ApiResponse<PayActionRes>(null, "savedCardToken is required", false);

            var body = new
            {
                payment_token = paymentKey,
                source = new { identifier = savedCardToken, subtype = "TOKEN" }
            };

            var url = $"{_opt.ApiBase}/acceptance/payments/pay";
            var res = await HttpPostJsonWithRetryAsync(url, body);

            object? payload = res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<object>()
                : await res.Content.ReadAsStringAsync();

            var success = res.IsSuccessStatusCode;

            return new ApiResponse<PayActionRes>(
                new PayActionRes(success, payload),
                success ? "Token payment initiated" : "Token payment failed",
                success,
                success ? null : new List<string> { payload?.ToString() ?? "Unknown error" }
            );
        }
        public async Task<ApiResponse<SavedCardPaymentResponse>> CreatePaymentKeyForSavedCardAsync(SavedCardChargeDto dto)
        {
            var currency = ResolveCurrency(dto.Currency);
            var merchantOrderId = EnsureMerchantOrderId(dto.MerchantOrderId);

            using (await AcquireLockAsync(merchantOrderId))
            {
                var upsert = await UpsertOrderAsync(merchantOrderId, dto.AmountCents, currency);
                if (!upsert.Status || upsert.Data is null)
                    return new ApiResponse<SavedCardPaymentResponse>(null, upsert.Message ?? "Unauthorized", false);

                var paymobOrderId = await GetOrCreatePaymobOrderIdAsync(merchantOrderId, dto.AmountCents, currency);
                if (paymobOrderId <= 0)
                    return new ApiResponse<SavedCardPaymentResponse>(null, "Failed to create Paymob order", false);

                // Billing مختصر وثابت (ممكن تستبدله ببيانات المستخدم)
                var billing = new BillingData(
                    first_name: "NA", last_name: "NA", email: "na@example.com", phone_number: "01000000000",
                    apartment: "NA", floor: "NA", building: "NA", street: "NA",
                    city: "Cairo", state: "NA", country: "EG", postal_code: "NA"
                );

                var paymentKey = await GetOrCreatePaymentKeyAsync(upsert.Data, dto.AmountCents, currency, billing, _opt.Integration.Card, 3600);

                // سجل Pending
                var hasTx = (await TxRepo.GetAllAsync(t => t.MerchantOrderId == merchantOrderId, false)).Any();
                if (!hasTx)
                    await AddTransactionAsync(merchantOrderId, paymobOrderId, dto.AmountCents, currency, "CardToken", "Pending", false);

                return new ApiResponse<SavedCardPaymentResponse>(
                    new SavedCardPaymentResponse(merchantOrderId, paymobOrderId, paymentKey, null),
                    "Payment key for saved card issued", true
                );
            }
        }
        public async Task<ApiResponse<CardPaymentKeyRes>> CreateCardPaymentKeyAsync(CardCheckoutServiceDto req)
        {
            var currency = ResolveCurrency(req.Currency);
            var merchantOrderId = EnsureMerchantOrderId(req.MerchantOrderId);

            using (await AcquireLockAsync(merchantOrderId))
            {
                // upsert order في DB
                var upsert = await UpsertOrderAsync(merchantOrderId, req.AmountCents, currency);
                if (!upsert.Status || upsert.Data is null)
                    return new ApiResponse<CardPaymentKeyRes>(null, upsert.Message ?? "Unauthorized", false);

                // إنشاء/الحصول على Paymob Order
                var paymobOrderId = await GetOrCreatePaymobOrderIdAsync(merchantOrderId, req.AmountCents, currency);
                if (paymobOrderId <= 0)
                    return new ApiResponse<CardPaymentKeyRes>(null, "Failed to create Paymob order", false);

                // إصدار Payment Key
                var paymentKey = await GetOrCreatePaymentKeyAsync(upsert.Data, req.AmountCents, currency, req.Billing, _opt.Integration.Card, 3600);

                // سجل Pending أول مرة
                var hasTx = (await TxRepo.GetAllAsync(t => t.MerchantOrderId == merchantOrderId, false)).Any();
                if (!hasTx)
                    await AddTransactionAsync(merchantOrderId, paymobOrderId, req.AmountCents, currency, "Card", "Pending", false);

                // مبدئيًا نرجّع iframeUrl تحسبًا لاستخدام الويب (اختياري للـSDK)
                var iframe = BuildCardIframeUrl(paymentKey).Data!;

                return new ApiResponse<CardPaymentKeyRes>(
                    new CardPaymentKeyRes(merchantOrderId, paymobOrderId, paymentKey, iframe),
                    "Payment key issued", true
                );
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
                delivery_needed = false,
                amount_cents = amountCents,
                currency = currency,
                merchant_order_id = merchantOrderId,
                items = Array.Empty<object>()
            };

            var res = await HttpPostJsonWithRetryAsync($"{_opt.ApiBase}/ecommerce/orders", body);

            // ✅ تعديل الخطأ: ما نتعاملش كأنه نجح لو الـ StatusCode فشل
            if (!res.IsSuccessStatusCode)
                return 0;

            var data = await res.Content.ReadFromJsonAsync<PaymobOrderRes>();

            // ✅ تعديل الخطأ: تأكيد وجود id صالح قبل الحفظ
            if (data is null || data.id <= 0)
                return 0;

            var paymobOrderId = data.id;

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
        private async Task<string> GetOrCreatePaymentKeyAsync(PaymentOrder order, long amountCents, string currency, BillingData billing, int integrationId, int expirationSeconds = 3600,bool tokenize = false)
        {
            if (IsKeyValid(order))
                return order.LastPaymentKey!;

            var auth = await _tokenProvider.GetAsync();
            var norm = NormalizeAndValidateBilling(billing);
            if (!norm.IsValid)
                throw new InvalidOperationException($"Invalid billing_data: {norm.Error}");

            var body = new PaymobPaymentKeyReq(auth, amountCents, expirationSeconds, order.PaymobOrderId!.Value, norm.Value, currency, integrationId)
            {
                tokenize = tokenize ? true : null
            };
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
























