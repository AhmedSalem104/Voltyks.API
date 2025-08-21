using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Input_DTOs
{
    // Inquiry
    public record InquiryDto(
        string AuthToken,
        string? MerchantOrderId = null,
        int? OrderId = null,
        long? TransactionId = null
    );
}
