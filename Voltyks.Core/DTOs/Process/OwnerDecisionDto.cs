using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.Core.DTOs.Process
{
    public class OwnerDecisionDto : ILocalizedRequest
    {
        public int ProcessId { get; set; }

        // القيم الجديدة:
        // “Process-Completed” | “Process-Ended-By-Report” | “Process-Started” | “Process-Aborted”
        public string Decision { get; set; } = "Process-Completed";

        /// <summary>Optional language for the notification ("en"/"ar"). Defaults to "en".</summary>
        public string? Lang { get; set; }
    }

}
