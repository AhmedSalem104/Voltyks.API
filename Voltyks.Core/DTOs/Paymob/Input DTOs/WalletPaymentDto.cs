using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Input_DTOs
{
    // Wallet Pay
    public record WalletPaymentDto(
        string PaymentToken,            // من Payment Key
        string? WalletPhone = null      // حسب نوع المحفظة
    );
}
