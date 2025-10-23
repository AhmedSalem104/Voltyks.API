using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Process
{
    public class ConfirmByVehicleOwnerDto
    {
        public int ChargerRequestId { get; set; }
        public decimal AmountCharged { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? EstimatedPrice { get; set; }
    }
}
