using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main.Paymob
{
    // RevokedCardToken.cs
    public class RevokedCardToken : BaseEntity<int>
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public string Token { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
