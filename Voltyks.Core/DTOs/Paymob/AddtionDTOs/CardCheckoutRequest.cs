using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public record CardCheckoutRequest(
      string MerchantOrderId,
    long AmountCents,
    string Currency,
    BillingData Billing
  );
}
