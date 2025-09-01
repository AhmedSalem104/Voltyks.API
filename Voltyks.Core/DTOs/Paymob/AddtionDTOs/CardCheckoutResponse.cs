using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public record CardCheckoutResponse(
      string MerchantOrderId,
    long PaymobOrderId,
    string PaymentKey,
    string IframeUrl
);
}
