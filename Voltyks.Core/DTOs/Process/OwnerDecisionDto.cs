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
        public string Decision { get; set; } = "Confirm"; // "Confirm" | "Report"
    }
}
