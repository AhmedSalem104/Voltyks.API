using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces.Paymob
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Voltyks.Core.DTOs;
    using Voltyks.Core.DTOs.ChargerRequest;
    using Voltyks.Core.DTOs.Paymob.AddtionDTOs;
    using Voltyks.Core.DTOs.Paymob.ApplePay;
    using Voltyks.Core.DTOs.Paymob.CardsDTOs;
    using Voltyks.Core.DTOs.Paymob.Generic_Result_DTOs;
    using Voltyks.Core.DTOs.Paymob.Input_DTOs;
    using Voltyks.Core.DTOs.Paymob.intention;
    using Voltyks.Persistence.Entities.Main.Paymob;

    public interface IPaymobService
    {

        // === Cards Tokenization & Management ===
        Task<ApiResponse<TokenizationStartRes>> StartCardTokenizationAsync(long amountCents = 0);
        Task<ApiResponse<IEnumerable<SavedCardViewDto>>> ListSavedCardsAsync();
        Task<ApiResponse<bool>> SetDefaultCardAsync(int cardId);
        Task<ApiResponse<bool>> DeleteCardAsync(int cardId);

        // === Charging with Saved Card (two styles) ===
        Task<ApiResponse<object>> ChargeWithSavedCardServerAsync(ChargeWithSavedCardReq req);

        // === Intention (Client Secret for SDK) ===
        Task<ApiResponse<CreateIntentResponse>> CreateIntentionAsync(CreateIntentRequest r, CancellationToken ct = default);

        Task<ApiResponse<CardPaymentKeyRes>> CreateCardPaymentKeyAsync(CardCheckoutServiceDto req);
        Task<ApiResponse<SavedCardPaymentResponse>> CreatePaymentKeyForSavedCardAsync(SavedCardChargeDto dto);
        Task<ApiResponse<PayActionRes>> PayWithSavedTokenAsync(string paymentKey, string savedCardToken);


        Task<ApiResponse<CardCheckoutResponse>> CheckoutCardAsync(CardCheckoutServiceDto req);
        Task<ApiResponse<WalletCheckoutResponse>> CheckoutWalletAsync(WalletCheckoutServiceDto req);
        Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody, bool skipHmac = false);
        Task<ApiResponse<OrderStatusDto>> GetOrderStatusFromPaymobAsync(long paymobOrderId);
        Task<ApiResponse<IntentionClientSecretDto>> ExchangePaymentKeyForClientSecretAsync(string paymentKey, string? publicKeyOverride = null, CancellationToken ct = default);


        // دالة لمعالجة Webhook
        //Task<ApiResponse<bool>> HandlePaymentNotificationAsync(HttpRequest req, string rawBody);

        // === Card Token Webhook Log Queries ===
        Task<ApiResponse<object>> GetCardWebhookLogsAsync(CardTokenStatus? status = null, string? userId = null, int page = 1, int pageSize = 20);
        Task<ApiResponse<CardTokenWebhookLogDetailDto?>> GetCardWebhookDetailAsync(int id);
        Task<ApiResponse<CardTokenWebhookStatsDto>> GetCardWebhookStatsAsync();

        // === Apple Pay Server-to-Server ===
        /// <summary>
        /// Process Apple Pay token directly from mobile app (Server-to-Server flow)
        /// </summary>
        /// <param name="request">Apple Pay request containing token from iOS PKPayment</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Apple Pay processing result with transaction details</returns>
        Task<ApiResponse<ApplePayProcessResponse>> ProcessApplePayAsync(ApplePayDirectRequest request, CancellationToken ct = default);

        /// <summary>
        /// Verify Apple Pay payment status by merchant order ID
        /// </summary>
        /// <param name="merchantOrderId">The merchant order ID from the original payment request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Current payment status</returns>
        Task<ApiResponse<ApplePayVerifyResponse>> VerifyApplePayAsync(string merchantOrderId, CancellationToken ct = default);

    }


}
