using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Input_DTOs
{
    // CreateOrder
    public record CreateOrderDto(
        string AuthToken,
        long AmountCents,
        string MerchantOrderId,
        string? Currency = null
    );
}
