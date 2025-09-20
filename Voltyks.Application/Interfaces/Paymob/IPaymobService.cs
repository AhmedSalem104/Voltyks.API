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
        Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody);
        Task<ApiResponse<OrderStatusDto>> GetOrderStatusFromPaymobAsync(long paymobOrderId);
        Task<ApiResponse<IntentionClientSecretDto>> ExchangePaymentKeyForClientSecretAsync(string paymentKey, string? publicKeyOverride = null, CancellationToken ct = default);


        // دالة لمعالجة Webhook
        //Task<ApiResponse<bool>> HandlePaymentNotificationAsync(HttpRequest req, string rawBody);





    }


}
