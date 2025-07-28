using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.ChargerRequest
{
    public class SendChargingRequestDto
    {
        public int ChargerId { get; set; }
        public string? DeviceToken { get; set; } // ✅ أضف دي

    }

}
