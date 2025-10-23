using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main
{
    public class RatingsHistory
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public string RaterUserId { get; set; } = default!;
        public string RateeUserId { get; set; } = default!;
        public double Stars { get; set; } // 1..5
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
