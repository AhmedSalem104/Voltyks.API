using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.CardsDTOs
{
    public record SavedCardInitChargeRes(
        string MerchantOrderId, long PaymobOrderId, string PaymentKey, string SavedToken
    );
}
