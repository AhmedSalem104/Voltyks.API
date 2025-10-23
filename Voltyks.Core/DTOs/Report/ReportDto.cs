using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Report
{
    public class ReportDto
    {
        public int ProcessId { get; set; }
        public string? UserId { get; set; }
        public DateTime? ReportDate { get; set; } = DateTime.Now;
        public bool? IsResolved { get; set; }
        public UserDetailDto UserDetails { get; set; }
        public string? ReportContent { get; set; }
    }

}
