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
    using Voltyks.Core.DTOs.Paymob.Generic_Result_DTOs;
    using Voltyks.Core.DTOs.Paymob.Input_DTOs;

    public interface IPaymobService
    {
        Task<ApiResponse<string>> GetAuthTokenAsync();
        Task<ApiResponse<int>> CreateOrderAsync(CreateOrderDto dto);
        Task<ApiResponse<string>> CreatePaymentKeyAsync(CreatePaymentKeyDto dto);
        ApiResponse<string> BuildCardIframeUrl(string paymentKey);
        Task<ApiResponse<PayActionRes>> PayWithWalletAsync(WalletPaymentDto dto);
        ApiResponse<bool> VerifyHmac(HmacVerifyDto dto);
        Task<ApiResponse<InquiryRes>> InquiryAsync(InquiryDto dto);
        Task<ApiResponse<PayActionRes>> RefundAsync(RefundDto dto);
        Task<ApiResponse<PayActionRes>> VoidAsync(VoidDto dto);
        Task<ApiResponse<PayActionRes>> CaptureAsync(CaptureDto dto);
        Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody);
    }


}
