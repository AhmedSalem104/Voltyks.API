using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class DeviceToken :BaseEntity<int>
    {

        public string UserId { get; set; }
        public AppUser User { get; set; }

        public string Token { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }

}
