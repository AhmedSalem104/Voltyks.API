using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;

namespace Voltyks.Core.DTOs.Paymob.Input_DTOs
{
    // CreatePaymentKey
    public record CreatePaymentKeyDto(
        string AuthToken,
        int OrderId,
        long AmountCents,
        BillingData Billing,
        int IntegrationId,              // Card/Wallet integration
        string? Currency = null,
        int ExpirationSeconds = 3600
    );
}
