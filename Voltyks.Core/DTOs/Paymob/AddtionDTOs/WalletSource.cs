using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{

    public sealed class WalletSource
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; init; } = "";

        [JsonPropertyName("subtype")]
        public string Subtype { get; init; } = "WALLET";
    }
}
