using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Input_DTOs
{
    // Void
    public record VoidDto(
        string AuthToken,
        long TransactionId,
        long AmountCents
    );
}
