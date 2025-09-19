using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.CardsDTOs
{
    public record SavedCardPaymentResponse(
     string MerchantOrderId,
     long PaymobOrderId,
     string PaymentKey,
     object? PayPayload // ممكن يكون response من Paymob أو null
 );

}
