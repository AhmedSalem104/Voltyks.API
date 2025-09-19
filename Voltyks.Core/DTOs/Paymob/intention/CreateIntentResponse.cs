using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.intention
{
    public record CreateIntentResponse(
        string ClientSecret,
        string PublicKey,
        string IntentionId,
        int IntentionOrderId,
        string? RedirectionUrl,
        string? Status,
        List<string>? PaymentKeys
    );


}
