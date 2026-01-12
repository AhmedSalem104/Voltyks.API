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
using Voltyks.Core.DTOs.Paymob.ApplePay;
using Twilio.TwiML.Messaging;
using Voltyks.Core.DTOs.Paymob.intention;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using System.Transactions;
using Voltyks.Persistence.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using Voltyks.Application.Interfaces.Redis;


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
        private readonly UserManager<AppUser> _userManager;
        private readonly IRedisService _redisService;


        private IGenericRepository<PaymentOrder, int> OrdersRepo => _uow.GetRepository<PaymentOrder, int>();
        private IGenericRepository<PaymentTransaction, int> TxRepo => _uow.GetRepository<PaymentTransaction, int>();
        //private IGenericRepository<UserSavedCard, int> SaveCardRepo => _uow.GetRepository<UserSavedCard, int>();

        // داخل الـService اللي فيه _uow
        private IGenericRepository<WebhookLog, int> WebhookLogs => _uow.GetRepository<WebhookLog, int>();
        private IGenericRepository<PaymentOrder, int> PaymentOrders => _uow.GetRepository<PaymentOrder, int>();
        private IGenericRepository<PaymentTransaction, int> PaymentTransactions => _uow.GetRepository<PaymentTransaction, int>();
        private IGenericRepository<UserSavedCard, int> SavedCards => _uow.GetRepository<UserSavedCard, int>();
        private IGenericRepository<CardTokenWebhookLog, int> CardTokenWebhookLogs => _uow.GetRepository<CardTokenWebhookLog, int>();

        public PaymobService(HttpClient http, IOptions<PaymobOptions> opt, IUnitOfWork uow, ILogger<PaymobService> log, IPaymobAuthTokenProvider tokenProvider, IHttpContextAccessor httpContext, IHttpClientFactory httpFactory, UserManager<AppUser> userManager, IRedisService redisService)
        {
            _http = http;
            _opt = opt.Value;
            _uow = uow;
            _log = log;
            _tokenProvider = tokenProvider;
            _httpContext = httpContext;
            _httpFactory = httpFactory;
            _userManager = userManager;
            _redisService = redisService;
        }
        public async Task<ApiResponse<CardCheckoutResponse>> CheckoutCardAsync(CardCheckoutServiceDto req)
        {
            // (1) Auth-token - validate it's available
            var authToken = await _tokenProvider.GetAsync();
            if (string.IsNullOrWhiteSpace(authToken))
            {
                _log?.LogError("Failed to obtain Paymob auth token for card checkout");
                return new ApiResponse<CardCheckoutResponse> { Status = false, Message = "Authentication failed with payment provider" };
            }

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
            // (1) Auth-token - validate it's available
            var authToken = await _tokenProvider.GetAsync();
            if (string.IsNullOrWhiteSpace(authToken))
            {
                _log?.LogError("Failed to obtain Paymob auth token for wallet checkout");
                return new ApiResponse<WalletCheckoutResponse> { Status = false, Message = "Authentication failed with payment provider" };
            }

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
                GetEgyptTime()
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
                var selectedMethod = r.PaymentMethod?.ToLowerInvariant() switch
                {
                    "wallet" => "Wallet",
                    "applepay" => "ApplePay",
                    _ => "Card"
                };

                int integrationId = selectedMethod switch
                {
                    "Wallet" => _opt.Integration.Wallet,
                    "ApplePay" => _opt.Integration.ApplePay,
                    _ => _opt.Integration.Card
                };

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

                var body = new
                {
                    amount = r.Amount,
                    currency,
                    payment_methods = new[] { integrationId },
                    billing_data = new
                    {
                        first_name = r.BillingData.First_Name,
                        last_name = r.BillingData.Last_Name,
                        email = r.BillingData.Email,
                        phone_number = r.BillingData.Phone_Number
                    },
                    special_reference = specialReference,
                    // notification_url removed - Paymob will use webhook configured in dashboard
                    tokenize = r.SaveCard,
                    merchant_order_id = $"uid:{upsert.Data!.UserId}|ord:{orderId}",
                    metadata = new { user_id = upsert.Data!.UserId }
                };


                // 5) Call Paymob Intention API
                var http = _httpFactory.CreateClient();
                using var req = new HttpRequestMessage(HttpMethod.Post, baseUrl);
                req.Headers.Accept.ParseAdd("application/json");
                req.Headers.TryAddWithoutValidation("Authorization", $"Token {secretKey}");
                // No CamelCase policy - preserve snake_case property names for Paymob
                req.Content = new StringContent(
                    JsonSerializer.Serialize(
                        body,
                        new JsonSerializerOptions
                        {
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


        //public async Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody)
        //{
        //    // [A] Log سريع
        //    _log?.LogWarning("Webhook arrived: {Method} {Path}{Query} UA={UA}",
        //        req.Method, req.Path, req.QueryString.Value, req.Headers["User-Agent"].ToString());

        //    // --------- 0) تجميع الحقول كما هي ----------
        //    var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        //    foreach (var kv in req.Query) fields[kv.Key] = kv.Value.ToString();

        //    try
        //    {
        //        if (req.HasFormContentType)
        //        {
        //            var form = await req.ReadFormAsync();
        //            foreach (var kv in form) fields[kv.Key] = kv.Value.ToString();
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
        //        }
        //    }
        //    catch { /* ignore parsing errors */ }

        //    // --------- 1) تحديد النوع مبكراً ----------
        //    string typeRaw = (FirstValue(fields, "type", "obj.type") ?? "TRANSACTION").Trim();
        //    string eventType = typeRaw.ToUpperInvariant();

        //    bool hasCardTokenKeys = FirstValue(fields,
        //        "obj.token", "obj.saved_card_token", "obj.card_token",
        //        "token", "saved_card_token", "card_token") != null;
        //    if (hasCardTokenKeys) eventType = "CARD_TOKEN";

        //    // --------- 2) استخراج بيانات البطاقة ---------
        //    if (eventType == "CARD_TOKEN" || eventType == "TOKEN")
        //    {
        //        var cardToken = FirstValue(fields, "obj.token", "obj.saved_card_token", "obj.card_token", "token", "saved_card_token", "card_token");
        //        string? last4 = FirstValue(fields, "obj.last4", "last4");

        //        // إذا لم يكن last4 موجودًا، نحاول استخراجه من masked_pan
        //        if (string.IsNullOrWhiteSpace(last4))
        //        {
        //            var masked = FirstValue(fields, "obj.masked_pan", "masked_pan", "obj.source_data.pan", "source_data.pan");
        //            if (!string.IsNullOrWhiteSpace(masked))
        //            {
        //                var digits = new string(masked.Where(char.IsDigit).ToArray());
        //                if (digits.Length >= 4) last4 = digits[^4..];
        //            }
        //        }

        //        string? brand = FirstValue(fields, "obj.card_subtype", "card_subtype", "obj.source_data.type", "source_data.type", "obj.brand", "brand");

        //        // استخراج MerchantId
        //        long? mid = TryParseLong(fields, "obj.merchant_id", "merchant_id", "obj.merchant.id", "merchant.id");

        //        // استخراج شهر وسنة انتهاء البطاقة (في حال كانت موجودة)
        //        int? expMonth = null, expYear = null;

        //        var mmStr = FirstValue(fields, "obj.expiry_month", "expiry_month", "obj.expiration_month", "expiration_month", "obj.card_expiry_month", "card_expiry_month", "obj.source_data.exp_month", "source_data.exp_month");
        //        var yyStr = FirstValue(fields, "obj.expiry_year", "expiry_year", "obj.expiration_year", "expiration_year", "obj.exp_year", "exp_year", "obj.card_expiry_year", "card_expiry_year", "obj.source_data.exp_year", "source_data.exp_year");

        //        // محاولة استخراج من حقل "expiry" إذا لم نجد الشهر والسنة بشكل منفصل
        //        if (string.IsNullOrWhiteSpace(mmStr) || string.IsNullOrWhiteSpace(yyStr))
        //        {
        //            var expiryCombined = FirstValue(fields, "obj.expiry", "expiry", "obj.expiration", "expiration", "obj.source_data.expiry", "source_data.expiry");
        //            if (!string.IsNullOrWhiteSpace(expiryCombined))
        //            {
        //                var m = System.Text.RegularExpressions.Regex.Match(expiryCombined, @"(?<mm>\d{1,2})\D+(?<yy>\d{2,4})");
        //                if (m.Success)
        //                {
        //                    mmStr = m.Groups["mm"].Value;
        //                    yyStr = m.Groups["yy"].Value;
        //                }
        //            }
        //        }

        //        // تحويل الشهر والسنة إلى أرقام إذا كانت موجودة
        //        if (int.TryParse(mmStr, out var mm)) expMonth = mm;
        //        if (int.TryParse(yyStr, out var yy)) expYear = yy >= 100 ? yy : (2000 + yy);

        //        if (expMonth is < 1 or > 12) expMonth = null;
        //        if (expYear is < 2000 or > 2100) expYear = null;

        //        // إذا كانت البطاقة موجودة أو جديدة، نقوم بحفظها مباشرة
        //        var userId = await ResolveUserIdFromTokenContextAsync(fields);
        //        if (string.IsNullOrWhiteSpace(cardToken) || string.IsNullOrWhiteSpace(userId))
        //        {
        //            _log?.LogWarning("CARD_TOKEN missing data → userId={userId}, token={token}", userId, cardToken);
        //            return new ApiResponse<bool> { Status = true, Message = "CARD_TOKEN ack (missing user/token)", Data = true };
        //        }

        //        var repoCards = _uow.GetRepository<UserSavedCard, int>();

        //        // تنظيف البيانات للتأكد من المقارنة بشكل دقيق
        //        cardToken = cardToken?.Trim().ToLower();
        //        last4 = last4?.Trim().ToLower();
        //        brand = brand?.Trim().ToLower();

        //        // تحقق إذا كانت البطاقة مكررة بناءً على UserId, last4, brand فقط
        //        var existing = await repoCards.GetFirstOrDefaultAsync(
        //            c => c.UserId == userId &&
        //                 c.Last4 == last4 &&
        //                 c.Brand == brand,
        //            trackChanges: false // لا نقوم بتتبع التغييرات هنا
        //        );

        //        // إذا كانت البطاقة مكررة، لا نقوم بحفظها
        //        if (existing != null)
        //        {
        //            _log?.LogWarning("Duplicate card → user={userId}, token={cardToken}");
        //            return new ApiResponse<bool> { Status = false, Message = "Duplicate card found", Data = false };
        //        }

        //        // إضافة بطاقة جديدة إذا لم تكن مكررة
        //        await SavedCards.AddAsync(new UserSavedCard
        //        {
        //            UserId = userId!,
        //            Token = cardToken!,
        //            Last4 = last4,
        //            Brand = brand,
        //            MerchantId = mid,
        //            ExpiryMonth = expMonth,
        //            ExpiryYear = expYear,
        //            CreatedAt = GetEgyptTime()
        //        });

        //        _log?.LogWarning("Saved NEW card → user={userId}, last4={last4}, brand={brand}, token={cardToken}", userId, last4, brand, cardToken);

        //        await _uow.SaveChangesAsync();
        //        return new ApiResponse<bool> { Status = true, Message = "CARD_TOKEN saved", Data = true };
        //    }

        //    // أي نوع حدث آخر
        //    return new ApiResponse<bool> { Status = true, Message = "Event processed", Data = true };
        //}
        public async Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody)
        {
            // [A] quick log
            _log?.LogWarning("Webhook arrived: {Method} {Path}{Query} UA={UA}",
                req.Method, req.Path, req.QueryString.Value, req.Headers["User-Agent"].ToString());

            // --------- 0) collect fields as-is (BEFORE HMAC check to detect event type) ----------
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
                }
            }
            catch (JsonException ex)
            {
                _log?.LogWarning(ex, "Failed to parse webhook JSON payload. Processing with available data.");
            }
            catch (Exception ex)
            {
                _log?.LogWarning(ex, "Unexpected error parsing webhook payload. Processing with available data.");
            }

            // --------- 1) early event type detection ----------
            string typeRaw = (FirstValue(fields, "type", "obj.type") ?? "TRANSACTION").Trim();
            string eventType = typeRaw.ToUpperInvariant();

            // broaden token key detection
            bool hasCardTokenKeys = FirstValue(fields,
                "obj.token", "obj.saved_card_token", "obj.card_token",
                "token", "saved_card_token", "card_token",
                "obj.source_data.token", "source_data.token",
                "obj.source_data.saved_card_token", "source_data.saved_card_token"
            ) != null;

            if (hasCardTokenKeys) eventType = "CARD_TOKEN";

            // --------- 1.5) LOG ALL INCOMING WEBHOOKS (before HMAC) ----------
            var merchantOrderId = FirstValue(fields, "obj.order.merchant_order_id", "merchant_order_id", "obj.merchant_order_id");
            var paymobOrderId = TryParseLong(fields, "obj.order.id", "order.id", "obj.order_id", "order_id");
            var paymobTxId = TryParseLong(fields, "obj.id", "id");
            var hmacFromQuery = req.Query["hmac"].FirstOrDefault();

            // Log to WebhookLogs for debugging (ALL webhooks, even failed HMAC)
            var webhookLog = new WebhookLog
            {
                EventType = eventType,
                MerchantOrderId = merchantOrderId,
                PaymobOrderId = paymobOrderId > 0 ? paymobOrderId : null,
                PaymobTransactionId = paymobTxId > 0 ? paymobTxId : null,
                IsHmacValid = false, // Will update after verification
                IsValid = false,
                HttpStatus = 200,
                HeadersJson = $"{{\"hmac\":\"{hmacFromQuery ?? "MISSING"}\",\"content-type\":\"{req.ContentType}\"}}",
                RawPayload = rawBody.Length > 10000 ? rawBody.Substring(0, 10000) + "...[TRUNCATED]" : rawBody,
                ReceivedAt = GetEgyptTime()
            };

            // --------- 2) verify HMAC with correct event type ----------
            var isHmacValid = VerifyWebhookSignature(req, rawBody, eventType);
            webhookLog.IsHmacValid = isHmacValid;

            if (!isHmacValid)
            {
                _log?.LogWarning("Invalid webhook signature for {EventType}. HMAC={Hmac}", eventType, hmacFromQuery ?? "MISSING");
                webhookLog.IsValid = false;
                try
                {
                    await WebhookLogs.AddAsync(webhookLog);
                    await _uow.SaveChangesAsync();
                    _log?.LogInformation("Logged failed HMAC webhook: EventType={EventType}, MerchantOrderId={MerchantOrderId}", eventType, merchantOrderId);
                }
                catch (Exception ex)
                {
                    _log?.LogError(ex, "Failed to log webhook to database");
                }
                return new ApiResponse<bool> { Status = true, Message = "Ignored (bad signature)", Data = true };
            }

            // HMAC valid - update log and save
            webhookLog.IsValid = true;
            try
            {
                await WebhookLogs.AddAsync(webhookLog);
                await _uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Failed to log valid webhook to database");
            }

            // --------- 3) TRANSACTION webhook handling ----------
            if (eventType == "TRANSACTION" || eventType == "TXN" ||
                (!hasCardTokenKeys && TryParseLong(fields, "obj.id", "id") > 0))
            {
                return await HandleTransactionWebhookAsync(fields);
            }

            if (eventType != "CARD_TOKEN" && eventType != "TOKEN")
            {
                _log?.LogInformation("Unknown event type: {EventType}. Known keys: {Keys}",
                    eventType, string.Join(", ", fields.Keys.Take(20).OrderBy(k => k)));
                return new ApiResponse<bool> { Status = true, Message = "Event acknowledged", Data = true };
            }

            // --------- 4) CARD_TOKEN webhook handling with full logging ----------
            return await HandleCardTokenWebhookAsync(fields, rawBody, isHmacValid: true);
        }

        /// <summary>
        /// Handle CARD_TOKEN webhook with full logging and idempotency
        /// </summary>
        private async Task<ApiResponse<bool>> HandleCardTokenWebhookAsync(
            Dictionary<string, string?> fields,
            string rawPayload,
            bool isHmacValid)
        {
            // 1. Generate unique webhook ID (for idempotency)
            var webhookId = GenerateCardTokenWebhookId(fields);

            // 2. Check if already processed (idempotency)
            var existingLog = await CardTokenWebhookLogs.GetFirstOrDefaultAsync(x => x.WebhookId == webhookId);
            if (existingLog != null)
            {
                _log?.LogInformation("CARD_TOKEN webhook already processed: {WebhookId}, Status: {Status}",
                    webhookId, existingLog.Status);
                return new ApiResponse<bool>(true, $"Already processed: {existingLog.Status}", true);
            }

            // 3. Create log entry immediately (Pending status)
            var logEntry = new CardTokenWebhookLog
            {
                WebhookId = webhookId,
                RawPayload = rawPayload,
                IsHmacValid = isHmacValid,
                Status = CardTokenStatus.Pending,
                ReceivedAt = GetEgyptTime()
            };

            // 4. HMAC validation already done, but log if it failed
            if (!isHmacValid)
            {
                logEntry.Status = CardTokenStatus.FailedHmac;
                logEntry.FailureReason = "HMAC signature verification failed";
                logEntry.ProcessedAt = GetEgyptTime();
                await CardTokenWebhookLogs.AddAsync(logEntry);
                await _uow.SaveChangesAsync();

                _log?.LogWarning("CARD_TOKEN rejected: HMAC invalid. WebhookId={WebhookId}", webhookId);
                return new ApiResponse<bool>(true, "HMAC invalid - logged", true);
            }

            // 5. Extract token
            var cardToken = FirstValue(fields,
                "obj.token", "obj.saved_card_token", "obj.card_token",
                "token", "saved_card_token", "card_token",
                "obj.source_data.token", "source_data.token",
                "obj.source_data.saved_card_token", "source_data.saved_card_token");

            logEntry.CardToken = cardToken?.Trim();

            if (string.IsNullOrWhiteSpace(cardToken))
            {
                logEntry.Status = CardTokenStatus.FailedNoToken;
                logEntry.FailureReason = "No token found in webhook payload";
                logEntry.ProcessedAt = GetEgyptTime();
                await CardTokenWebhookLogs.AddAsync(logEntry);
                await _uow.SaveChangesAsync();

                _log?.LogWarning("CARD_TOKEN missing token. WebhookId={WebhookId}", webhookId);
                return new ApiResponse<bool>(true, "No token - logged", true);
            }

            // 6. Extract card details
            logEntry.Last4 = ExtractLast4FromFields(fields);
            logEntry.Brand = ExtractBrandFromFields(fields);
            (logEntry.ExpiryMonth, logEntry.ExpiryYear) = ExtractExpiryFromFields(fields);

            // 7. Resolve user ID
            var userId = await ResolveUserIdFromTokenContextAsync(fields);
            logEntry.UserId = userId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                logEntry.Status = CardTokenStatus.FailedNoUser;
                logEntry.FailureReason = "Could not resolve UserId from webhook context";
                logEntry.ProcessedAt = GetEgyptTime();
                await CardTokenWebhookLogs.AddAsync(logEntry);
                await _uow.SaveChangesAsync();

                _log?.LogWarning("CARD_TOKEN missing user. WebhookId={WebhookId}", webhookId);
                return new ApiResponse<bool>(true, "No user - logged", true);
            }

            // 8. Check for duplicate card (same token for same user - more accurate)
            var existingCard = await SavedCards.GetFirstOrDefaultAsync(
                c => c.UserId == userId && c.Token == cardToken.Trim());

            if (existingCard != null)
            {
                logEntry.Status = CardTokenStatus.Duplicate;
                logEntry.FailureReason = $"Card with same token already exists (CardId: {existingCard.Id})";
                logEntry.SavedCardId = existingCard.Id;
                logEntry.ProcessedAt = GetEgyptTime();
                await CardTokenWebhookLogs.AddAsync(logEntry);
                await _uow.SaveChangesAsync();

                _log?.LogInformation("CARD_TOKEN duplicate. WebhookId={WebhookId}, ExistingCardId={CardId}",
                    webhookId, existingCard.Id);
                return new ApiResponse<bool>(true, "Duplicate - logged", true);
            }

            // 9. Save new card
            try
            {
                var newCard = new UserSavedCard
                {
                    UserId = userId,
                    Token = cardToken.Trim(),
                    Last4 = logEntry.Last4,
                    Brand = logEntry.Brand,
                    ExpiryMonth = logEntry.ExpiryMonth,
                    ExpiryYear = logEntry.ExpiryYear,
                    MerchantId = TryParseLong(fields, "obj.merchant_id", "merchant_id", "obj.merchant.id", "merchant.id"),
                    CreatedAt = GetEgyptTime()
                };

                await SavedCards.AddAsync(newCard);
                await _uow.SaveChangesAsync();

                // Update log entry with success
                logEntry.Status = CardTokenStatus.Saved;
                logEntry.SavedCardId = newCard.Id;
                logEntry.ProcessedAt = GetEgyptTime();
                await CardTokenWebhookLogs.AddAsync(logEntry);
                await _uow.SaveChangesAsync();

                _log?.LogInformation(
                    "CARD_TOKEN saved successfully. WebhookId={WebhookId}, UserId={UserId}, CardId={CardId}, Last4={Last4}",
                    webhookId, userId, newCard.Id, logEntry.Last4);

                return new ApiResponse<bool>(true, "Card saved", true);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                logEntry.Status = CardTokenStatus.Duplicate;
                logEntry.FailureReason = "Unique constraint violation (race condition)";
                logEntry.ProcessedAt = GetEgyptTime();
                await CardTokenWebhookLogs.AddAsync(logEntry);
                await _uow.SaveChangesAsync();

                _log?.LogInformation("CARD_TOKEN race condition duplicate. WebhookId={WebhookId}", webhookId);
                return new ApiResponse<bool>(true, "Duplicate (race) - logged", true);
            }
            catch (Exception ex)
            {
                logEntry.Status = CardTokenStatus.FailedDatabase;
                logEntry.FailureReason = $"Database error: {ex.Message}";
                logEntry.ProcessedAt = GetEgyptTime();
                await CardTokenWebhookLogs.AddAsync(logEntry);
                await _uow.SaveChangesAsync();

                _log?.LogError(ex, "CARD_TOKEN database error. WebhookId={WebhookId}", webhookId);
                return new ApiResponse<bool>(true, "DB error - logged", true);
            }
        }

        /// <summary>
        /// Generate unique webhook ID for idempotency
        /// </summary>
        private string GenerateCardTokenWebhookId(Dictionary<string, string?> fields)
        {
            var txnId = FirstValue(fields, "obj.id", "id", "obj.order.id", "order.id");
            var token = FirstValue(fields, "obj.token", "token");

            if (!string.IsNullOrWhiteSpace(txnId))
                return $"CARD_TOKEN_{txnId}";
            if (!string.IsNullOrWhiteSpace(token))
                return $"CARD_TOKEN_{token.GetHashCode():X8}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            return $"CARD_TOKEN_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Extract last 4 digits from webhook fields
        /// </summary>
        private string? ExtractLast4FromFields(Dictionary<string, string?> fields)
        {
            var last4 = FirstValue(fields, "obj.last4", "last4");

            // fallback from masked pan
            if (string.IsNullOrWhiteSpace(last4))
            {
                var masked = FirstValue(fields, "obj.masked_pan", "masked_pan", "obj.source_data.pan", "source_data.pan");
                if (!string.IsNullOrWhiteSpace(masked))
                {
                    var digits = new string(masked.Where(char.IsDigit).ToArray());
                    if (digits.Length >= 4) last4 = digits[^4..];
                }
            }

            if (string.IsNullOrWhiteSpace(last4)) return null;

            var digitsOnly = new string(last4.Where(char.IsDigit).ToArray());
            return digitsOnly.Length >= 4 ? digitsOnly[^4..] : (digitsOnly.Length > 0 ? digitsOnly : null);
        }

        /// <summary>
        /// Extract card brand from webhook fields
        /// </summary>
        private string? ExtractBrandFromFields(Dictionary<string, string?> fields)
        {
            var brand = FirstValue(fields,
                "obj.card_subtype", "card_subtype",
                "obj.source_data.type", "source_data.type",
                "obj.brand", "brand");
            return brand?.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Extract expiry month and year from webhook fields
        /// </summary>
        private (int? month, int? year) ExtractExpiryFromFields(Dictionary<string, string?> fields)
        {
            int? expMonth = null, expYear = null;
            var mmStr = FirstValue(fields, "obj.expiry_month", "expiry_month",
                                             "obj.expiration_month", "expiration_month",
                                             "obj.card_expiry_month", "card_expiry_month",
                                             "obj.source_data.exp_month", "source_data.exp_month");
            var yyStr = FirstValue(fields, "obj.expiry_year", "expiry_year",
                                             "obj.expiration_year", "expiration_year",
                                             "obj.exp_year", "exp_year",
                                             "obj.card_expiry_year", "card_expiry_year",
                                             "obj.source_data.exp_year", "source_data.exp_year");

            if (string.IsNullOrWhiteSpace(mmStr) || string.IsNullOrWhiteSpace(yyStr))
            {
                var expiryCombined = FirstValue(fields, "obj.expiry", "expiry",
                                                         "obj.expiration", "expiration",
                                                         "obj.source_data.expiry", "source_data.expiry");
                if (!string.IsNullOrWhiteSpace(expiryCombined))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(expiryCombined, @"(?<mm>\d{1,2})\D+(?<yy>\d{2,4})");
                    if (m.Success)
                    {
                        mmStr = m.Groups["mm"].Value;
                        yyStr = m.Groups["yy"].Value;
                    }
                }
            }

            if (int.TryParse(mmStr, out var mm)) expMonth = mm;
            if (int.TryParse(yyStr, out var yy)) expYear = yy >= 100 ? yy : (2000 + yy);

            if (expMonth is < 1 or > 12) expMonth = null;
            if (expYear is < 2000 or > 2100) expYear = null;

            return (expMonth, expYear);
        }


        private bool VerifyWebhookSignature(HttpRequest req, string rawBody, string eventType = "TRANSACTION")
        {
            // Get HMAC from query string (Paymob sends it as ?hmac=...)
            var hmacFromPaymob = req.Query["hmac"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(hmacFromPaymob))
            {
                _log?.LogWarning("Webhook received without HMAC signature");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_opt.HmacSecret))
            {
                _log?.LogError("HMAC secret not configured - cannot verify webhook");
                return false;
            }

            try
            {
                // Parse the JSON body to extract fields
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                // Get the obj element (Paymob wraps transaction data in "obj")
                if (!root.TryGetProperty("obj", out var obj))
                {
                    _log?.LogWarning("Webhook payload missing 'obj' element");
                    return false;
                }

                // Use correct concat method based on event type
                // CARD_TOKEN/TOKEN uses alphabetically sorted values
                // TRANSACTION uses specific fields in exact order
                var concatenated = (eventType == "CARD_TOKEN" || eventType == "TOKEN")
                    ? BuildTokenConcat(obj)
                    : BuildTransactionConcat(obj);

                // Calculate HMAC-SHA512
                using var hmac = new HMACSHA512(KeyFromHexOrUtf8(_opt.HmacSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
                var calculatedHmac = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                // Time-safe comparison to prevent timing attacks
                var isValid = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hmacFromPaymob.ToLowerInvariant()),
                    Encoding.UTF8.GetBytes(calculatedHmac));

                if (!isValid)
                {
                    _log?.LogWarning("HMAC verification failed for webhook. EventType={EventType}", eventType);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Error during HMAC verification");
                return false;
            }
        }

        /// <summary>
        /// Handle TRANSACTION webhooks from Paymob to update payment status
        /// </summary>
        private async Task<ApiResponse<bool>> HandleTransactionWebhookAsync(IDictionary<string, string?> fields)
        {
            // Extract transaction details
            var paymobTxId = TryParseLong(fields, "obj.id", "id");
            var paymobOrderId = TryParseLong(fields, "obj.order.id", "obj.order_id", "order.id", "order_id");
            var merchantOrderId = FirstValue(fields, "obj.order.merchant_order_id", "obj.merchant_order_id", "merchant_order_id");
            var amountCents = TryParseLong(fields, "obj.amount_cents", "amount_cents");
            var currency = FirstValue(fields, "obj.currency", "currency") ?? "EGP";

            // Payment status
            bool isSuccess = TryParseBool(fields, "obj.success") || TryParseBool(fields, "success");
            bool isPending = TryParseBool(fields, "obj.pending") || TryParseBool(fields, "pending");
            bool isVoided = TryParseBool(fields, "obj.is_voided") || TryParseBool(fields, "is_voided");
            bool isRefunded = TryParseBool(fields, "obj.is_refunded") || TryParseBool(fields, "is_refunded");

            // Determine status string
            string status;
            if (isSuccess) status = "Paid";
            else if (isPending) status = "Pending";
            else if (isVoided) status = "Voided";
            else if (isRefunded) status = "Refunded";
            else status = "Failed";

            _log?.LogInformation("TRANSACTION webhook: txId={TxId}, orderId={OrderId}, merchantOrderId={MerchantOrderId}, status={Status}, success={Success}",
                paymobTxId, paymobOrderId, merchantOrderId, status, isSuccess);

            // Try to find order by merchant_order_id or paymob_order_id
            PaymentOrder? order = null;

            if (!string.IsNullOrWhiteSpace(merchantOrderId))
            {
                // Try extracting actual orderId from format "uid:xxx|ord:yyy"
                var match = System.Text.RegularExpressions.Regex.Match(merchantOrderId, @"ord:([A-Za-z0-9_\-]+)");
                var actualOrderId = match.Success ? match.Groups[1].Value : merchantOrderId;

                order = await PaymentOrders.GetFirstOrDefaultAsync(
                    o => o.MerchantOrderId == merchantOrderId || o.MerchantOrderId == actualOrderId,
                    trackChanges: true);
            }

            if (order == null && paymobOrderId > 0)
            {
                order = await PaymentOrders.GetFirstOrDefaultAsync(
                    o => o.PaymobOrderId == paymobOrderId,
                    trackChanges: true);
            }

            if (order == null)
            {
                _log?.LogWarning("TRANSACTION webhook: Order not found. merchantOrderId={MerchantOrderId}, paymobOrderId={OrderId}",
                    merchantOrderId, paymobOrderId);
                // ACK anyway to prevent retries
                return new ApiResponse<bool> { Status = true, Message = "Order not found - acknowledged", Data = true };
            }

            // Update order status
            order.Status = status;
            order.UpdatedAt = GetEgyptTime();
            if (paymobOrderId > 0 && !order.PaymobOrderId.HasValue)
                order.PaymobOrderId = paymobOrderId;

            PaymentOrders.Update(order);

            // Update or create transaction record
            var existingTx = paymobTxId > 0
                ? await PaymentTransactions.GetFirstOrDefaultAsync(t => t.PaymobTransactionId == paymobTxId, trackChanges: true)
                : null;

            if (existingTx != null)
            {
                existingTx.Status = status;
                existingTx.IsSuccess = isSuccess;
                existingTx.HmacVerified = true;
                existingTx.UpdatedAt = GetEgyptTime();
                PaymentTransactions.Update(existingTx);
            }
            else
            {
                // Try to find by merchant_order_id
                var txByOrderId = await PaymentTransactions.GetFirstOrDefaultAsync(
                    t => t.MerchantOrderId == order.MerchantOrderId,
                    trackChanges: true);

                if (txByOrderId != null)
                {
                    txByOrderId.PaymobTransactionId = paymobTxId > 0 ? paymobTxId : txByOrderId.PaymobTransactionId;
                    txByOrderId.Status = status;
                    txByOrderId.IsSuccess = isSuccess;
                    txByOrderId.HmacVerified = true;
                    txByOrderId.UpdatedAt = GetEgyptTime();
                    PaymentTransactions.Update(txByOrderId);
                }
                else if (paymobTxId > 0)
                {
                    // Create new transaction record
                    var newTx = new PaymentTransaction
                    {
                        PaymobTransactionId = paymobTxId,
                        PaymobOrderId = paymobOrderId > 0 ? paymobOrderId : null,
                        MerchantOrderId = order.MerchantOrderId,
                        AmountCents = amountCents > 0 ? amountCents : order.AmountCents,
                        Currency = currency,
                        Status = status,
                        IsSuccess = isSuccess,
                        HmacVerified = true,
                        IntegrationType = "Webhook",
                        CreatedAt = GetEgyptTime()
                    };
                    await PaymentTransactions.AddAsync(newTx);
                }
            }

            try
            {
                await _uow.SaveChangesAsync();
                _log?.LogInformation("TRANSACTION webhook processed: order={OrderId} updated to {Status}",
                    order.MerchantOrderId, status);
                return new ApiResponse<bool> { Status = true, Message = $"Transaction {status}", Data = true };
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Failed to save transaction webhook changes");
                // ACK anyway
                return new ApiResponse<bool> { Status = true, Message = "Error handled", Data = true };
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            // SQL Server: 2601 or 2627
            if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                return true;

            // PostgreSQL: 23505
            if (ex.InnerException?.GetType().Name == "PostgresException" &&
                ex.InnerException?.GetType().GetProperty("SqlState")?.GetValue(ex.InnerException)?.ToString() == "23505")
                return true;

            // MySQL: 1062 or message contains duplicate/unique
            var msg = ex.InnerException?.Message ?? ex.Message;
            if (msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("1062"))
                return true;

            return false;
        }



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

                if (!root.TryGetProperty("client_secret", out var clientSecretProp) ||
                    clientSecretProp.ValueKind != JsonValueKind.String)
                {
                    _log?.LogError("Missing or invalid client_secret in Intention response");
                    return new ApiResponse<IntentionClientSecretDto>
                    {
                        Status = false,
                        Message = "Invalid response - missing client_secret",
                        Errors = new List<string> { raw }
                    };
                }

                var clientSecret = clientSecretProp.GetString();

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

                if (!root.TryGetProperty("token", out var tokenProp) ||
                    tokenProp.ValueKind != JsonValueKind.String)
                {
                    _log?.LogError("Missing or invalid token in auth response");
                    throw new InvalidOperationException("Failed to get auth token from Paymob - invalid response");
                }

                return tokenProp.GetString()!;
            }
            else
            {
                _log?.LogError("Failed to get Bearer Token: {Error}", raw);
                throw new Exception($"Failed to get Bearer Token: {raw}");
            }
        }
        public string CalculateHmac(object body, string secret)
        {
            var bodyString = JsonSerializer.Serialize(body);

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(bodyString));

            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public bool CompareHmacSignature(string calculatedSignature, string receivedSignature)
        {
            // Use time-safe comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(calculatedSignature.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(receivedSignature.ToLowerInvariant()));
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

            try
            {
                if (k.Data.PaymobOrderId > 0)
                {
                    var statusRes = await GetOrderStatusFromPaymobAsync(k.Data.PaymobOrderId);
                    if (statusRes.Status && statusRes.Data is not null)
                        await ApplyOrderStatusAsync(statusRes.Data, fallbackMerchantOrderId: merchantOrderId);
                }
            }
            catch (DbUpdateException ex)
            {
                return new ApiResponse<object>(
                    null,
                    "DB update failed while applying order status.",
                    false,
                    new List<string> { ex.Message, ex.InnerException?.Message ?? "(no inner)" }
                );
            }




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
        private async Task ApplyOrderStatusAsync(OrderStatusDto dto, string? fallbackMerchantOrderId = null)
        {
            var mOrderId = !string.IsNullOrWhiteSpace(dto.MerchantOrderId)
                ? dto.MerchantOrderId!
                : (fallbackMerchantOrderId ?? "");

            if (string.IsNullOrWhiteSpace(mOrderId))
            {
                _log?.LogWarning("ApplyOrderStatusAsync: empty merchantOrderId.");
                return;
            }

            // خرّج حالة موحّدة لجدولك
            var localStatus =
                dto.IsSuccess
                || string.Equals(dto.TransactionStatus, "paid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dto.OrderStatus, "paid", StringComparison.OrdinalIgnoreCase)
                    ? "Paid"
                    : string.Equals(dto.OrderStatus, "pending", StringComparison.OrdinalIgnoreCase)
                        ? "pending"
                        : "Failed";

            // 1) حدّث الأوردر أولاً (ولو فشل، ارجع برسالة واضحة)
            try
            {
                await UpdateOrderStatusAsync(mOrderId, localStatus);
                // لو UpdateOrderStatusAsync لا يعمل Save داخليًا، فعّل السطر التالي:
                // await _uow.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _log?.LogError(ex,
                    "DB error while updating order. merchantOrderId={mOrderId}, status={status}, inner={inner}",
                    mOrderId, localStatus, ex.InnerException?.Message);

                throw; // خليه يظهر فوق في ApiResponse عشان تشوف الـ inner
            }

            // 2) (اختياري) حدّث الترانزاكشن لو متاح — ولو فشل ما تفشلش الطلب كله
            if (dto.PaymobTransactionId is long txId && txId > 0)
            {
                try
                {
                    await UpdateTransactionByPaymobIdAsync(txId, tx =>
                    {
                        tx.PaymobOrderId = dto.PaymobOrderId;
                        tx.MerchantOrderId = mOrderId;
                        if (dto.AmountCents > 0) tx.AmountCents = dto.AmountCents;
                        if (!string.IsNullOrWhiteSpace(dto.Currency)) tx.Currency = dto.Currency!;
                        tx.IsSuccess = localStatus == "Paid";
                        tx.Status = localStatus;
                        tx.HmacVerified = true;
                    });

                    await MarkProcessedTxAsync(txId, "SYNCED_FROM_GET_ORDER_STATUS");
                }
                catch (DbUpdateException ex)
                {
                    _log?.LogError(ex,
                        "DB error while updating transaction. txId={txId}, inner={inner}",
                        txId, ex.InnerException?.Message);
                    // متعمّد ما نرميش الاستثناء هنا عشان الأوردر اتحدّث بالفعل
                }
            }
        }
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
        private async Task MarkProcessedTxAsync(long paymobTransactionId, string evtType)
        {
            await WebhookLogs.AddAsync(new WebhookLog
            {
                EventType = evtType, // "TRANSACTION_PROCESSED"
                PaymobTransactionId = paymobTransactionId,
                IsHmacValid = true,
                IsValid = true,
                HttpStatus = 200,
                ReceivedAt = GetEgyptTime()
            });
            await _uow.SaveChangesAsync();
        }
        
        private async Task UpdateOrderStatusAsync(string merchantOrderId, string status)
        {
            var order = await PaymentOrders.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);
            if (order is null) return;

            order.Status = status;
            order.UpdatedAt = GetEgyptTime();

            PaymentOrders.Update(order);
            await _uow.SaveChangesAsync();
        }
        // ===== تحديث المعاملة =====
        private async Task UpdateTransactionByPaymobIdAsync(long paymobTransactionId, Action<PaymentTransaction> mutate)
        {
            var tx = await PaymentTransactions.GetFirstOrDefaultAsync(t => t.PaymobTransactionId == paymobTransactionId, trackChanges: true);
            if (tx is null) return;

            mutate(tx);
            tx.UpdatedAt = GetEgyptTime();

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
            int attempt = 0;
            while (attempt <= maxRetries)
            {
                var res = await _http.PostAsJsonAsync(url, body, ct);

                // Success or non-retryable error
                if (res.StatusCode != (HttpStatusCode)429)
                    return res;

                // Max retries reached
                if (attempt >= maxRetries)
                {
                    _log?.LogWarning("Max retries ({MaxRetries}) reached for {Url}", maxRetries, url);
                    return res;
                }

                attempt++;
                var delay = GetRetryDelay(res, attempt);
                _log?.LogWarning("429 @ {Url} retry in {Delay}s (attempt {Attempt}/{Max})",
                    url, delay.TotalSeconds, attempt, maxRetries);
                await Task.Delay(delay, ct);
            }

            // This should never be reached, but return a failed response just in case
            throw new InvalidOperationException("Retry loop exited unexpectedly");
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
            order.UpdatedAt = GetEgyptTime();
            OrdersRepo.Update(order);
            await _uow.SaveChangesAsync();

            return paymobOrderId;
        }
        private bool IsKeyValid(PaymentOrder o)
            => !string.IsNullOrWhiteSpace(o.LastPaymentKey)
               && o.PaymentKeyExpiresAt.HasValue
               && o.PaymentKeyExpiresAt.Value > GetEgyptTime().AddSeconds(30);
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

            // Validate HTTP response
            if (!res.IsSuccessStatusCode)
            {
                var errorContent = await res.Content.ReadAsStringAsync();
                _log?.LogError("Payment key creation failed: {Status} - {Body}", res.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to create payment key: {res.StatusCode}");
            }

            var data = await res.Content.ReadFromJsonAsync<PaymobPaymentKeyRes>();

            // Validate response data
            if (data?.token == null)
            {
                _log?.LogError("Payment key response was null or missing token");
                throw new InvalidOperationException("Failed to generate payment key - invalid response");
            }

            var key = data.token;
            order.LastPaymentKey = key;
            order.PaymentKeyExpiresAt = GetEgyptTime().AddSeconds(expirationSeconds);
            order.Status = "Pending";
            order.UpdatedAt = GetEgyptTime();
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
                    CreatedAt = GetEgyptTime(),
                    UserId = currentUserId
                };
                await OrdersRepo.AddAsync(order);
            }
            else
            {
                order.AmountCents = amountCents;
                order.Currency = currency;
                order.UpdatedAt = GetEgyptTime();

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
                CreatedAt = GetEgyptTime()
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
        //        order.UpdatedAt = GetEgyptTime();
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
        //        tx.UpdatedAt = GetEgyptTime();
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
        public static DateTime GetEgyptTime()
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptZone);
        }

        #region Card Token Webhook Log Queries

        /// <summary>
        /// Get paginated list of card token webhook logs
        /// </summary>
        public async Task<ApiResponse<object>> GetCardWebhookLogsAsync(
            CardTokenStatus? status = null,
            string? userId = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = CardTokenWebhookLogs.Query();

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(x => x.UserId == userId);

            var total = await query.CountAsync();
            var rawItems = await query
                .OrderByDescending(x => x.ReceivedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs with masked token (can't use range in EF query)
            var items = rawItems.Select(x => new CardTokenWebhookLogDto
            {
                Id = x.Id,
                WebhookId = x.WebhookId,
                UserId = x.UserId,
                CardToken = MaskCardToken(x.CardToken),
                Last4 = x.Last4,
                Brand = x.Brand,
                ExpiryMonth = x.ExpiryMonth,
                ExpiryYear = x.ExpiryYear,
                Status = x.Status.ToString(),
                StatusCode = (int)x.Status,
                FailureReason = x.FailureReason,
                IsHmacValid = x.IsHmacValid,
                ReceivedAt = x.ReceivedAt,
                ProcessedAt = x.ProcessedAt,
                SavedCardId = x.SavedCardId
            }).ToList();

            var result = new
            {
                items,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            };

            return new ApiResponse<object>(result, "OK", true);
        }

        /// <summary>
        /// Get detailed card token webhook log including raw payload
        /// </summary>
        public async Task<ApiResponse<CardTokenWebhookLogDetailDto?>> GetCardWebhookDetailAsync(int id)
        {
            var log = await CardTokenWebhookLogs.GetFirstOrDefaultAsync(x => x.Id == id);

            if (log == null)
                return new ApiResponse<CardTokenWebhookLogDetailDto?>(null, "Not found", false);

            var dto = new CardTokenWebhookLogDetailDto
            {
                Id = log.Id,
                WebhookId = log.WebhookId,
                UserId = log.UserId,
                CardToken = log.CardToken,
                Last4 = log.Last4,
                Brand = log.Brand,
                ExpiryMonth = log.ExpiryMonth,
                ExpiryYear = log.ExpiryYear,
                Status = log.Status.ToString(),
                StatusCode = (int)log.Status,
                FailureReason = log.FailureReason,
                IsHmacValid = log.IsHmacValid,
                ReceivedAt = log.ReceivedAt,
                ProcessedAt = log.ProcessedAt,
                SavedCardId = log.SavedCardId,
                RawPayload = log.RawPayload
            };

            return new ApiResponse<CardTokenWebhookLogDetailDto?>(dto, "OK", true);
        }

        /// <summary>
        /// Get statistics for card token webhooks
        /// </summary>
        public async Task<ApiResponse<CardTokenWebhookStatsDto>> GetCardWebhookStatsAsync()
        {
            var query = CardTokenWebhookLogs.Query();

            var stats = new CardTokenWebhookStatsDto
            {
                Total = await query.CountAsync(),
                Saved = await query.CountAsync(x => x.Status == CardTokenStatus.Saved),
                Duplicate = await query.CountAsync(x => x.Status == CardTokenStatus.Duplicate),
                FailedNoUser = await query.CountAsync(x => x.Status == CardTokenStatus.FailedNoUser),
                FailedNoToken = await query.CountAsync(x => x.Status == CardTokenStatus.FailedNoToken),
                FailedHmac = await query.CountAsync(x => x.Status == CardTokenStatus.FailedHmac),
                FailedDatabase = await query.CountAsync(x => x.Status == CardTokenStatus.FailedDatabase)
            };

            // Get top failure reasons
            stats.TopFailureReasons = await query
                .Where(x => x.FailureReason != null)
                .GroupBy(x => x.FailureReason!)
                .Select(g => new FailureReasonCount
                {
                    Reason = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            return new ApiResponse<CardTokenWebhookStatsDto>(stats, "OK", true);
        }

        /// <summary>
        /// Mask card token for display (show only last 8 chars)
        /// </summary>
        private static string? MaskCardToken(string? token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            return token.Length > 8 ? $"***{token[^8..]}" : $"***{token}";
        }

        #endregion

        #region Apple Pay Server-to-Server

        /// <summary>
        /// Process Apple Pay token directly from mobile app (Server-to-Server flow)
        /// Flow: Create Order → Create Payment Key → Send Apple Pay Token to Paymob → Return Result
        /// </summary>
        public async Task<ApiResponse<ApplePayProcessResponse>> ProcessApplePayAsync(ApplePayDirectRequest request, CancellationToken ct = default)
        {
            var response = new ApplePayProcessResponse
            {
                AmountCents = request.AmountCents,
                Currency = request.Currency,
                ProcessedAt = GetEgyptTime()
            };

            try
            {
                // 1. Validate input
                if (string.IsNullOrWhiteSpace(request.ApplePayToken))
                {
                    _log?.LogWarning("Apple Pay: Empty token received");
                    response.Success = false;
                    response.Status = "Failed";
                    response.ErrorCode = "EMPTY_TOKEN";
                    response.ErrorMessage = "Apple Pay token is required";
                    return new ApiResponse<ApplePayProcessResponse>(response, "Apple Pay token is required", false);
                }

                if (request.AmountCents < 100)
                {
                    _log?.LogWarning("Apple Pay: Invalid amount {Amount}", request.AmountCents);
                    response.Success = false;
                    response.Status = "Failed";
                    response.ErrorCode = "INVALID_AMOUNT";
                    response.ErrorMessage = "Amount must be at least 100 cents";
                    return new ApiResponse<ApplePayProcessResponse>(response, "Invalid amount", false);
                }

                _log?.LogInformation("Apple Pay: Starting payment processing for {Amount} cents", request.AmountCents);

                // 2. Generate stable merchant order ID first (needed for idempotency)
                var merchantOrderId = Guid.NewGuid().ToString("N");
                var currency = request.Currency ?? "EGP";
                response.MerchantOrderId = merchantOrderId;

                // 3. Idempotency check - prefer client-provided key, fallback to stable merchantOrderId
                var idempotencyKey = !string.IsNullOrWhiteSpace(request.IdempotencyKey)
                    ? request.IdempotencyKey
                    : $"applepay:{merchantOrderId}";
                var redisKey = $"applepay_idempotency:{idempotencyKey}";

                var existingResult = await _redisService.GetAsync(redisKey);
                if (!string.IsNullOrEmpty(existingResult))
                {
                    _log?.LogWarning("Apple Pay: Duplicate request detected for idempotency key (key masked)");
                    try
                    {
                        var cached = JsonSerializer.Deserialize<ApiResponse<ApplePayProcessResponse>>(existingResult);
                        if (cached != null) return cached;
                    }
                    catch { /* Continue if deserialization fails */ }
                }

                // 4. Get Paymob auth token
                var authToken = await _tokenProvider.GetAsync();
                if (string.IsNullOrWhiteSpace(authToken))
                {
                    _log?.LogError("Apple Pay: Failed to get Paymob auth token");
                    response.Success = false;
                    response.Status = "Failed";
                    response.ErrorCode = "AUTH_FAILED";
                    response.ErrorMessage = "Failed to authenticate with payment provider";
                    return new ApiResponse<ApplePayProcessResponse>(response, "Authentication failed", false);
                }

                // 5. Create DB order with already-generated merchantOrderId
                using (await AcquireLockAsync(merchantOrderId))
                {
                    var upsertResult = await UpsertOrderAsync(merchantOrderId, request.AmountCents, currency);
                    if (!upsertResult.Status || upsertResult.Data == null)
                    {
                        _log?.LogError("Apple Pay: Failed to create order in DB");
                        response.Success = false;
                        response.Status = "Failed";
                        response.ErrorCode = "ORDER_CREATION_FAILED";
                        response.ErrorMessage = "Failed to create payment order";
                        return new ApiResponse<ApplePayProcessResponse>(response, "Order creation failed", false);
                    }

                    // 6. Create Paymob Order
                    var paymobOrderId = await GetOrCreatePaymobOrderIdAsync(merchantOrderId, request.AmountCents, currency);
                    if (paymobOrderId <= 0)
                    {
                        _log?.LogError("Apple Pay: Failed to create Paymob order");
                        response.Success = false;
                        response.Status = "Failed";
                        response.ErrorCode = "PAYMOB_ORDER_FAILED";
                        response.ErrorMessage = "Failed to create payment order with provider";
                        return new ApiResponse<ApplePayProcessResponse>(response, "Paymob order creation failed", false);
                    }
                    response.PaymobOrderId = paymobOrderId;

                    _log?.LogInformation("Apple Pay: Created Paymob order {OrderId}", paymobOrderId);

                    // 7. Create Payment Key with Apple Pay integration
                    var billing = new BillingData(
                        first_name: request.BillingData.first_name ?? "NA",
                        last_name: request.BillingData.last_name ?? "NA",
                        email: request.BillingData.email ?? "na@example.com",
                        phone_number: request.BillingData.phone_number ?? "00000000000",
                        apartment: "NA", floor: "NA", building: "NA", street: "NA",
                        city: "Cairo", state: "NA", country: "EG", postal_code: "NA"
                    );

                    var paymentKey = await GetOrCreatePaymentKeyAsync(
                        upsertResult.Data,
                        request.AmountCents,
                        currency,
                        billing,
                        _opt.Integration.ApplePay, // Use Apple Pay integration ID
                        3600, // 1 hour expiry
                        tokenize: false
                    );

                    if (string.IsNullOrWhiteSpace(paymentKey))
                    {
                        _log?.LogError("Apple Pay: Failed to create payment key");
                        response.Success = false;
                        response.Status = "Failed";
                        response.ErrorCode = "PAYMENT_KEY_FAILED";
                        response.ErrorMessage = "Failed to create payment key";
                        return new ApiResponse<ApplePayProcessResponse>(response, "Payment key creation failed", false);
                    }

                    _log?.LogInformation("Apple Pay: Created payment key for order {OrderId}", paymobOrderId);

                    // 8. Process Apple Pay token with Paymob (SECURITY: Never log applePayToken)
                    var applePayResult = await SendApplePayTokenToPaymobAsync(paymentKey, request.ApplePayToken, ct);

                    if (!applePayResult.Status || applePayResult.Data == null)
                    {
                        _log?.LogError("Apple Pay: Paymob rejected the token - {Message}", applePayResult.Message);
                        response.Success = false;
                        response.Status = "Failed";
                        response.ErrorCode = "PAYMOB_REJECTED";
                        response.ErrorMessage = applePayResult.Message ?? "Payment provider rejected the transaction";

                        // Record failed transaction
                        await AddTransactionAsync(merchantOrderId, paymobOrderId, request.AmountCents, currency, "ApplePay", "Failed", false);

                        return new ApiResponse<ApplePayProcessResponse>(response, applePayResult.Message ?? "Apple Pay failed", false);
                    }

                    // 9. Parse Paymob response - only mark Paid if confirmed (success=true AND pending=false)
                    var paymobResponse = applePayResult.Data;
                    response.TransactionId = paymobResponse.TransactionId;

                    var isConfirmedPaid = paymobResponse.Success && !paymobResponse.IsPending;
                    var isFailed = !paymobResponse.Success && !paymobResponse.IsPending;

                    response.Success = isConfirmedPaid;
                    response.IsPending = !isConfirmedPaid && !isFailed;
                    response.Status = isConfirmedPaid ? "Paid"
                                    : isFailed ? "Failed"
                                    : "Pending";
                    response.Message = isConfirmedPaid
                        ? "Payment successful"
                        : isFailed
                            ? (paymobResponse.Message ?? "Payment failed")
                            : "Payment initiated - awaiting confirmation";

                    // 10. Record transaction in DB (Paid only if confirmed)
                    var txStatus = response.Status;
                    await AddTransactionAsync(merchantOrderId, paymobOrderId, request.AmountCents, currency, "ApplePay", txStatus, isConfirmedPaid);

                    _log?.LogInformation("Apple Pay: Transaction completed - Status: {Status}, TxId: {TxId}",
                        txStatus, response.TransactionId);

                    // 11. Cache result for idempotency (15 min TTL)
                    var result = new ApiResponse<ApplePayProcessResponse>(response, response.Message, isConfirmedPaid || response.IsPending);
                    try
                    {
                        var cacheJson = JsonSerializer.Serialize(result);
                        await _redisService.SetAsync(redisKey, cacheJson, TimeSpan.FromMinutes(15));
                    }
                    catch (Exception cacheEx)
                    {
                        _log?.LogWarning(cacheEx, "Apple Pay: Failed to cache result for idempotency");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Apple Pay: Unexpected error during payment processing");
                response.Success = false;
                response.Status = "Failed";
                response.ErrorCode = "INTERNAL_ERROR";
                response.ErrorMessage = "An unexpected error occurred";
                return new ApiResponse<ApplePayProcessResponse>(response, "Internal error", false)
                {
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Send Apple Pay token to Paymob for processing
        /// Paymob API: POST /api/acceptance/payments/pay
        /// SECURITY: Never log the applePayToken - it contains sensitive payment data
        /// </summary>
        private async Task<ApiResponse<ApplePayProcessResponse>> SendApplePayTokenToPaymobAsync(
            string paymentKey,
            string applePayToken,
            CancellationToken ct = default)
        {
            var response = new ApplePayProcessResponse();

            try
            {
                // Paymob Apple Pay endpoint
                var url = $"{_opt.ApiBase.TrimEnd('/')}/api/acceptance/payments/pay";

                // Build request payload
                // According to Paymob docs, Apple Pay token should be sent as-is
                var payload = new
                {
                    source = new
                    {
                        identifier = applePayToken,
                        subtype = "APPLE_PAY"
                    },
                    payment_token = paymentKey
                };

                // No naming policy - keeps property names as-is (snake_case for Paymob)
                var json = JsonSerializer.Serialize(payload);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // SECURITY: Only log URL, never log applePayToken or payload contents
                _log?.LogInformation("Apple Pay: Sending request to Paymob - URL: {Url}", url);

                var httpResponse = await _http.SendAsync(httpRequest, ct);
                var responseBody = await httpResponse.Content.ReadAsStringAsync(ct);

                _log?.LogInformation("Apple Pay: Paymob response status {Status}, body length: {Length}",
                    httpResponse.StatusCode, responseBody.Length);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _log?.LogError("Apple Pay: Paymob API error - Status: {Status}, Body: {Body}",
                        httpResponse.StatusCode, responseBody);

                    response.Success = false;
                    response.Status = "Failed";
                    response.ErrorCode = $"HTTP_{(int)httpResponse.StatusCode}";
                    response.ErrorMessage = $"Paymob API error: {httpResponse.StatusCode}";

                    // Try to parse error message from response
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseBody);
                        if (errorDoc.RootElement.TryGetProperty("message", out var msgEl))
                        {
                            response.ErrorMessage = msgEl.GetString() ?? response.ErrorMessage;
                        }
                        else if (errorDoc.RootElement.TryGetProperty("detail", out var detailEl))
                        {
                            response.ErrorMessage = detailEl.GetString() ?? response.ErrorMessage;
                        }
                    }
                    catch { /* Ignore parsing errors */ }

                    return new ApiResponse<ApplePayProcessResponse>(response, response.ErrorMessage, false);
                }

                // Parse successful response
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                // Extract transaction details
                if (root.TryGetProperty("id", out var idEl))
                {
                    response.TransactionId = idEl.GetInt64().ToString();
                }

                if (root.TryGetProperty("success", out var successEl))
                {
                    response.Success = successEl.GetBoolean();
                }

                if (root.TryGetProperty("pending", out var pendingEl))
                {
                    response.IsPending = pendingEl.GetBoolean();
                }

                if (root.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("message", out var dataMsgEl))
                {
                    response.Message = dataMsgEl.GetString();
                }

                // Check for error in response
                if (root.TryGetProperty("txn_response_code", out var txnCodeEl))
                {
                    var code = txnCodeEl.GetString();
                    if (!string.IsNullOrEmpty(code) && code != "APPROVED")
                    {
                        response.Success = false;
                        response.ErrorCode = code;
                    }
                }

                response.Status = response.Success ? "Paid" : (response.IsPending ? "Pending" : "Failed");
                response.ProcessedAt = GetEgyptTime();

                return new ApiResponse<ApplePayProcessResponse>(response,
                    response.Success ? "Payment successful" : "Payment failed",
                    response.Success);
            }
            catch (HttpRequestException ex)
            {
                _log?.LogError(ex, "Apple Pay: HTTP error communicating with Paymob");
                response.Success = false;
                response.Status = "Failed";
                response.ErrorCode = "NETWORK_ERROR";
                response.ErrorMessage = "Network error communicating with payment provider";
                return new ApiResponse<ApplePayProcessResponse>(response, "Network error", false);
            }
            catch (JsonException ex)
            {
                _log?.LogError(ex, "Apple Pay: Failed to parse Paymob response");
                response.Success = false;
                response.Status = "Failed";
                response.ErrorCode = "PARSE_ERROR";
                response.ErrorMessage = "Failed to parse payment provider response";
                return new ApiResponse<ApplePayProcessResponse>(response, "Parse error", false);
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Apple Pay: Unexpected error sending token to Paymob");
                response.Success = false;
                response.Status = "Failed";
                response.ErrorCode = "INTERNAL_ERROR";
                response.ErrorMessage = "Internal error processing payment";
                return new ApiResponse<ApplePayProcessResponse>(response, "Internal error", false);
            }
        }

        /// <summary>
        /// Verify Apple Pay payment status by merchant order ID
        /// </summary>
        public async Task<ApiResponse<ApplePayVerifyResponse>> VerifyApplePayAsync(string merchantOrderId, CancellationToken ct = default)
        {
            var response = new ApplePayVerifyResponse { MerchantOrderId = merchantOrderId };

            try
            {
                if (string.IsNullOrWhiteSpace(merchantOrderId))
                {
                    return new ApiResponse<ApplePayVerifyResponse>(response, "Merchant order ID is required", false);
                }

                // Get order from DB
                var order = await PaymentOrders.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId);
                if (order == null)
                {
                    _log?.LogWarning("Apple Pay Verify: Order not found for merchantOrderId {OrderId}", merchantOrderId);
                    return new ApiResponse<ApplePayVerifyResponse>(response, "Order not found", false);
                }

                response.PaymobOrderId = order.PaymobOrderId;
                response.AmountCents = order.AmountCents;
                response.Currency = order.Currency ?? "EGP";
                response.Status = order.Status ?? "Unknown";
                response.IsPaid = order.Status == "Paid";
                response.IsPending = order.Status == "Pending";

                // Get latest transaction
                var tx = await PaymentTransactions.GetFirstOrDefaultAsync(t => t.MerchantOrderId == merchantOrderId);
                if (tx != null)
                {
                    response.TransactionId = tx.PaymobTransactionId?.ToString();
                    response.PaidAt = tx.Status == "Paid" ? tx.UpdatedAt : null;
                    response.FailureReason = tx.Status == "Failed" ? tx.GatewayResponseMessage : null;
                }

                _log?.LogInformation("Apple Pay Verify: Status={Status}, IsPaid={IsPaid}, IsPending={IsPending} for merchantOrderId {OrderId}",
                    response.Status, response.IsPaid, response.IsPending, merchantOrderId);

                return new ApiResponse<ApplePayVerifyResponse>(response, "Success", true);
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Apple Pay Verify: Error verifying payment status for merchantOrderId {OrderId}", merchantOrderId);
                return new ApiResponse<ApplePayVerifyResponse>(response, "Error verifying payment status", false)
                {
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #endregion


    }
}
























