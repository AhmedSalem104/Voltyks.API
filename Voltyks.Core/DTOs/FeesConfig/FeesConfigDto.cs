using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.FeesConfig
{
    public class FeesConfigDto
    {
        public decimal MinimumFee { get; set; }
        public decimal Percentage { get; set; }

        // للعرض فقط
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
