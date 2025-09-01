using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Options
{
    public class PaymobOptions
    {
        public string ApiBase { get; set; } = default!;
        public string ApiKey { get; set; } = default!;
        public string HmacSecret { get; set; } = default!;
        public string IframeId { get; set; } = default!;
        public string Currency { get; set; } = "EGP";
        public IntegrationIds Integration { get; set; } = new();


       

    }
}
