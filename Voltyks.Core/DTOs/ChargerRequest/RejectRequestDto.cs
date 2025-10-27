using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.ChargerRequest
{
    public class RejectRequestDto
    {
        [JsonPropertyName("requestIds")]
        public List<RequestIdDto> RequestIds { get; set; }
    }

}
