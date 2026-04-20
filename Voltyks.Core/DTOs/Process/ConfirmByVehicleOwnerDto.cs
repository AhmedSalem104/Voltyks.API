using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.Core.DTOs.Process
{
    public class ConfirmByVehicleOwnerDto : ILocalizedRequest
    {
        public int ChargerRequestId { get; set; }
        public decimal AmountCharged { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? EstimatedPrice { get; set; }

        /// <summary>Optional language for the notification ("en"/"ar"). Defaults to "en".</summary>
        public string? Lang { get; set; }
    }
}
