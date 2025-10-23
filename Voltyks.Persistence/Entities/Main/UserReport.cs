using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class UserReport : BaseEntity<int>
    {
       
        public int ProcessId { get; set; }
        public string UserId { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;
        public string ReportContent { get; set; }
        public virtual AppUser? User { get; set; }
        public virtual Process? Process { get; set; }
    }

}
