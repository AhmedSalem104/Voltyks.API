using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main
{
    public class UsersBanned :BaseEntity<int>
    {
        public string UserId { get; set; }
        public DateTime BannedDate { get; set; }
        public DateTime? BanExpiryDate { get; set; } // تاريخ انتهاء الحظر (اختياري)
        public string BanReason { get; set; } // سبب الحظر (اختياري)
    }

}
