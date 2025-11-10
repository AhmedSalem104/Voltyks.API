using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Process
{
    public class OwnerDecisionDto
    {
        public int ProcessId { get; set; }

        // القيم الجديدة:
        // “Process-Completed” | “Process-Ended-By-Report” | “Process-Started” | “Process-Aborted”
        public string Decision { get; set; } = "Process-Completed";
    }

}
