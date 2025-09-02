using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Paymob.AddtionDTOs;

namespace Voltyks.Core.DTOs.Paymob.Core_API_DTOs
{
    public sealed class WalletPayReq
    {
        [JsonPropertyName("source")]
        public WalletSource Source { get; init; } = new();

        [JsonPropertyName("payment_token")]
        public string PaymentToken { get; init; } = "";
    }
}
