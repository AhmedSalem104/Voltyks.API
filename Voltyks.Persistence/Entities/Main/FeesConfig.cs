using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Utilities;

namespace Voltyks.Persistence.Entities.Main
{
    public class FeesConfig : BaseEntity<int>
    {
        // ex: 40.00
        public decimal MinimumFee { get; set; }

        // ex: 10.00 (يعني 10%)
        public decimal Percentage { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTimeHelper.GetEgyptTime();
        public string? UpdatedBy { get; set; }
    }

}
