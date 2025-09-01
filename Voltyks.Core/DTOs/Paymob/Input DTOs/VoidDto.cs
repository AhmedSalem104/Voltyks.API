using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Input_DTOs
{
    // Void
    public record VoidDto(long TransactionId, long AmountCents, string? AuthToken = null);

}
