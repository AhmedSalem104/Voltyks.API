using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public record WalletOnlyResponse(string MerchantOrderId, bool Paid, long AmountCents, string Currency);

}
