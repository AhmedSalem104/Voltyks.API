using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Report
{
    public class ReportFilterDto
    {
        public string? UserId { get; set; } // فلترة حسب المستخدم (اختياري)
        public DateTime? StartDate { get; set; } // فلترة حسب تاريخ البداية (اختياري)
        public DateTime? EndDate { get; set; } // فلترة حسب تاريخ النهاية (اختياري)
    }

}
