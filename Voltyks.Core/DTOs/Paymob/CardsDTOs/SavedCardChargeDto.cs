using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.CardsDTOs
{
    public record SavedCardChargeDto(
     long AmountCents,
     string? Currency,
     string SavedCardToken,
     string? MerchantOrderId
 );

}
