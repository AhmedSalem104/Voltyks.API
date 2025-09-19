using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.CardsDTOs
{
    public class ChargeWithSavedCardReq
    {
        public int CardId { get; set; }
        public long AmountCents { get; set; }
        public string? Currency { get; set; } = "EGP";
        public string? MerchantOrderId { get; set; }
    }
}
