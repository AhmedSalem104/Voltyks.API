using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public record WalletCheckoutRequest(
      string MerchantOrderId,
    long AmountCents,
    string Currency,
    string WalletPhone
 );
}
