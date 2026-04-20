using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.Core.DTOs.Process
{
    public class UpdateProcessDto : ILocalizedRequest
    {
        public int ProcessId { get; set; }
        public string? Status { get; set; } // "Process-Completed" | "Process-Aborted" | ...
        public decimal? EstimatedPrice { get; set; }
        public decimal? AmountCharged { get; set; }
        public decimal? AmountPaid { get; set; }

        /// <summary>Optional language for the notification ("en"/"ar"). Defaults to "en".</summary>
        public string? Lang { get; set; }
    }


}
