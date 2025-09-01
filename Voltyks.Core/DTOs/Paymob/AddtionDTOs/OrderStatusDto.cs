using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public record OrderStatusDto(
    string MerchantOrderId,
    string? OrderStatus,
    string? LastTransactionStatus,
    bool IsSuccess,
    long AmountCents,
    string Currency,
    long? PaymobOrderId,
    long? PaymobTransactionId,
    DateTime UpdatedAt
 );
}
