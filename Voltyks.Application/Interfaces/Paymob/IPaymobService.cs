using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces.Paymob
{
    using System.Threading.Tasks;
    using Voltyks.Core.DTOs.Paymob.Generic_Result_DTOs;
    using Voltyks.Core.DTOs.Paymob.Input_DTOs;

    public interface IPaymobService
    {
        // 1) استرجاع Auth Token
        Task<string> GetAuthTokenAsync();

        // 2) Create Order
        Task<int> CreateOrderAsync(CreateOrderDto dto);

        // 3) Create Payment Key
        Task<string> CreatePaymentKeyAsync(CreatePaymentKeyDto dto);

        // 4) بناء رابط iFrame للبطاقات
        string BuildCardIframeUrl(string paymentKey);

        // 5) دفع المحافظ
        Task<PayActionRes> PayWithWalletAsync(WalletPaymentDto dto);

        // 6) التحقق من HMAC
        bool VerifyHmac(HmacVerifyDto dto);

        // 7) Inquiry
        Task<InquiryRes> InquiryAsync(InquiryDto dto);

        // 8) Refund
        Task<PayActionRes> RefundAsync(RefundDto dto);

        // 9) Void
        Task<PayActionRes> VoidAsync(VoidDto dto);

        // 10) Capture
        Task<PayActionRes> CaptureAsync(CaptureDto dto);
    }

}
