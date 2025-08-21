using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Input_DTOs
{
    // HMAC Verify
    public record HmacVerifyDto(
        string MessageToSign,           // يُبنى حسب ترتيب الحقول في Paymob docs
        string ReceivedHexSignature
    );
}
