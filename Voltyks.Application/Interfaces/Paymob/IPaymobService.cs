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
    using Voltyks.Core.DTOs.Paymob.AddtionDTOs;
    using Voltyks.Core.DTOs.Paymob.Generic_Result_DTOs;
    using Voltyks.Core.DTOs.Paymob.Input_DTOs;

    public interface IPaymobService
    {


        Task<ApiResponse<CardCheckoutResponse>> CheckoutCardAsync(CardCheckoutServiceDto req);
        Task<ApiResponse<WalletCheckoutResponse>> CheckoutWalletAsync(WalletCheckoutServiceDto req);
        Task<ApiResponse<bool>> HandleWebhookAsync(HttpRequest req, string rawBody);
        Task<ApiResponse<OrderStatusDto>> GetOrderStatusFromPaymobAsync(long paymobOrderId);

    }


}
