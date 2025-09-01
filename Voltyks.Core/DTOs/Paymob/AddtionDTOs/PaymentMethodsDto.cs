using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public record PaymentMethodsDto(bool Card, bool ApplePay, bool MobileWallet, bool WalletOnly, bool Cash);

}
