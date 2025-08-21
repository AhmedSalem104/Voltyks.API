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
namespace Voltyks.Application.Services.Paymob
{




    using System.Net.Http.Json;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Voltyks.Infrastructure.UnitOfWork;
    using Voltyks.Infrastructure;

    // using Voltyks.Infrastructure.UnitOfWork;           // IUnitOfWork
    // using Voltyks.Infrastructure.Interfaces;           // IGenericRepository<,>
    // using Voltyks.Domain.Entities.Payments;            // PaymentOrder, PaymentTransaction, WebhookLog, PaymentAction

    public class PaymobService : IPaymobService
    {
        private readonly HttpClient _http;
        private readonly PaymobOptions _opt;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PaymobService> _log;

        // NOTE: غيّري نوع المفتاح TKey لو الكيان عندك مفتاحه مش int
        private IGenericRepository<PaymentOrder, int> OrdersRepo
            => _uow.GetRepository<PaymentOrder, int>();

        private IGenericRepository<PaymentTransaction, int> TxRepo
            => _uow.GetRepository<PaymentTransaction, int>();

        private IGenericRepository<WebhookLog, int> WebhookRepo
            => _uow.GetRepository<WebhookLog, int>();

        private IGenericRepository<PaymentAction, int> ActionRepo
            => _uow.GetRepository<PaymentAction, int>();

        public PaymobService(
            HttpClient http,
            IOptions<PaymobOptions> opt,
            IUnitOfWork uow,
            ILogger<PaymobService> log)
        {
            _http = http;
            _opt = opt.Value;
            _uow = uow;
            _log = log;
        }

       
        // ============== 1) Auth ==============
        public async Task<string> GetAuthTokenAsync()
        {
            var res = await _http.PostAsJsonAsync($"{_opt.ApiBase}/auth/tokens", new PaymobAuthReq(_opt.ApiKey));
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<PaymobAuthRes>();
            return data!.token;
        }

        // ============== 2) Create Order ==============
        public async Task<int> CreateOrderAsync(CreateOrderDto dto)
        {
            var currency = dto.Currency ?? _opt.Currency;

            // Upsert محلي
            await UpsertOrderAsync(dto.MerchantOrderId, dto.AmountCents, currency);

            // اتصال Paymob
            var body = new PaymobOrderReq(
                auth_token: dto.AuthToken,
                amount_cents: dto.AmountCents,
                currency: currency,
                merchant_order_id: dto.MerchantOrderId,
                items: Array.Empty<object>());

            var res = await _http.PostAsJsonAsync($"{_opt.ApiBase}/ecommerce/orders", body);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<PaymobOrderRes>();
            var paymobOrderId = data!.id;

            // تحديث محلي
            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == dto.MerchantOrderId, trackChanges: true);
            if (order != null)
            {
                order.PaymobOrderId = paymobOrderId;
                order.Status = "OrderCreated";
                order.UpdatedAt = DateTime.UtcNow;
                OrdersRepo.Update(order);
                await _uow.SaveChangesAsync();
            }

            return paymobOrderId;
        }

        // ============== 3) Payment Key ==============
        public async Task<string> CreatePaymentKeyAsync(CreatePaymentKeyDto dto)
        {
            var currency = dto.Currency ?? _opt.Currency;

            var body = new PaymobPaymentKeyReq(
                auth_token: dto.AuthToken,
                amount_cents: dto.AmountCents,
                expiration: dto.ExpirationSeconds,
                order_id: dto.OrderId,
                billing_data: dto.Billing,
                currency: currency,
                integration_id: dto.IntegrationId);

            var res = await _http.PostAsJsonAsync($"{_opt.ApiBase}/acceptance/payment_keys", body);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<PaymobPaymentKeyRes>();
            var paymentKey = data!.token;

            // تحديث حالة الطلب + إنشاء محاولة معاملة
            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.PaymobOrderId == dto.OrderId, trackChanges: true);
            if (order != null)
            {
                order.Status = "PaymentKeyCreated";
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
                    isSuccess: false);
            }

            return paymentKey;
        }

        // ============== 4) iFrame URL ==============
        public string BuildCardIframeUrl(string paymentKey)
            => $"https://accept.paymob.com/api/acceptance/iframes/{_opt.IFrameId}?payment_token={paymentKey}";

        // ============== 5) Wallet Pay ==============
        public async Task<PayActionRes> PayWithWalletAsync(WalletPaymentDto dto)
        {
            var req = new WalletPayReq("WALLET", dto.PaymentToken, _opt.Integration.Wallet, dto.WalletPhone);
            var res = await _http.PostAsJsonAsync($"{_opt.ApiBase}/acceptance/payments/pay", req);

            object? payload = res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<object>()
                : await res.Content.ReadAsStringAsync();

            // لو قدرتي تستخرجي transaction_id من payload، نادِ UpdateTransactionByPaymobIdAsync هنا
            return new PayActionRes(res.IsSuccessStatusCode, payload);
        }

        // ============== 6) HMAC Verify ==============
        public bool VerifyHmac(HmacVerifyDto dto)
        {
            var secret = _opt.HmacSecret ?? throw new InvalidOperationException("HMAC secret not set");
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.MessageToSign));
            var hex = Convert.ToHexString(hash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hex),
                Encoding.UTF8.GetBytes(dto.ReceivedHexSignature.ToLowerInvariant())
            );
        }

        // ============== 7) Inquiry ==============
        public async Task<InquiryRes> InquiryAsync(InquiryDto dto)
        {
            var url = $"{_opt.ApiBase}/acceptance/transactions/";
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(dto.MerchantOrderId)) qs.Add($"merchant_order_id={Uri.EscapeDataString(dto.MerchantOrderId)}");
            if (dto.OrderId.HasValue) qs.Add($"order_id={dto.OrderId.Value}");
            if (dto.TransactionId.HasValue) qs.Add($"transaction_id={dto.TransactionId.Value}");
            var full = url + (qs.Count > 0 ? $"?{string.Join("&", qs)}" : string.Empty);

            using var req = new HttpRequestMessage(HttpMethod.Get, full);
            req.Headers.Add("Authorization", $"Bearer {dto.AuthToken}");
            var res = await _http.SendAsync(req);

            object? data = res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<object>()
                : await res.Content.ReadAsStringAsync();

            // بعد ما تعملي parsing للـ response، تقدري تحدّثي الـ Transaction/Order عبر الـ Repos
            return new InquiryRes(data);
        }

        // ============== 8) Refund ==============
        public async Task<PayActionRes> RefundAsync(RefundDto dto)
        {
            var body = new RefundReq(dto.AuthToken, dto.TransactionId, dto.AmountCents);
            var res = await _http.PostAsJsonAsync($"{_opt.ApiBase}/acceptance/void_refund/refund", body);

            object? data = res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<object>()
                : await res.Content.ReadAsStringAsync();

            await ActionRepo.AddAsync(new PaymentAction
            {
                ActionType = "Refund",
                PaymobTransactionId = dto.TransactionId,
                RequestedAmountCents = dto.AmountCents,
                Status = res.IsSuccessStatusCode ? "Succeeded" : "Failed",
                CreatedAt = DateTime.UtcNow
            });

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
            return new PayActionRes(res.IsSuccessStatusCode, data);
        }

        // ============== 9) Void ==============
        public async Task<PayActionRes> VoidAsync(VoidDto dto)
        {
            var body = new VoidOrCaptureReq(dto.AuthToken, dto.TransactionId, dto.AmountCents);
            var res = await _http.PostAsJsonAsync($"{_opt.ApiBase}/acceptance/void_refund/void", body);

            object? data = res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<object>()
                : await res.Content.ReadAsStringAsync();

            await ActionRepo.AddAsync(new PaymentAction
            {
                ActionType = "Void",
                PaymobTransactionId = dto.TransactionId,
                RequestedAmountCents = dto.AmountCents,
                Status = res.IsSuccessStatusCode ? "Succeeded" : "Failed",
                CreatedAt = DateTime.UtcNow
            });

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
            return new PayActionRes(res.IsSuccessStatusCode, data);
        }

        // ============== 10) Capture ==============
        public async Task<PayActionRes> CaptureAsync(CaptureDto dto)
        {
            var body = new VoidOrCaptureReq(dto.AuthToken, dto.TransactionId, dto.AmountCents);
            var res = await _http.PostAsJsonAsync($"{_opt.ApiBase}/acceptance/capture", body);

            object? data = res.IsSuccessStatusCode
                ? await res.Content.ReadFromJsonAsync<object>()
                : await res.Content.ReadAsStringAsync();

            await ActionRepo.AddAsync(new PaymentAction
            {
                ActionType = "Capture",
                PaymobTransactionId = dto.TransactionId,
                RequestedAmountCents = dto.AmountCents,
                Status = res.IsSuccessStatusCode ? "Succeeded" : "Failed",
                CreatedAt = DateTime.UtcNow
            });

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
            return new PayActionRes(res.IsSuccessStatusCode, data);
        }










        // ============== Helpers (Repository-based) ==============
        private async Task<PaymentOrder> UpsertOrderAsync(string merchantOrderId, long amountCents, string currency)
        {
            var order = await OrdersRepo.GetFirstOrDefaultAsync(o => o.MerchantOrderId == merchantOrderId, trackChanges: true);
            if (order is null)
            {
                order = new PaymentOrder
                {
                    MerchantOrderId = merchantOrderId,
                    AmountCents = amountCents,
                    Currency = currency,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
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

        private async Task AddTransactionAsync(
            string merchantOrderId,
            long? paymobOrderId,
            long amountCents,
            string currency,
            string integrationType,
            string status = "Initiated",
            bool isSuccess = false)
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

    }

}
