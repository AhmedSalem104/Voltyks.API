using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public record WalletCheckoutResponse(
       string MerchantOrderId,
    long PaymobOrderId,
    bool Started,
    object? Data,
    string? RedirectUrl,
    string? Reference
  );
}
