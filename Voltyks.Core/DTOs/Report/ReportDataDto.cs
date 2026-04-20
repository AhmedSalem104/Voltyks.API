using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.Core.DTOs.Report
{
    public class ReportDataDto : ILocalizedRequest
    {
        public int ProcessId { get; set; }
        public string? ReportContent { get; set; }

        /// <summary>Optional language for the notification ("en"/"ar"). Defaults to "en".</summary>
        public string? Lang { get; set; }
    }
}
