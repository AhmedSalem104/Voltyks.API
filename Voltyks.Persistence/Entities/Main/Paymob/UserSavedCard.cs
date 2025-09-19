using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main.Paymob
{
    public class UserSavedCard : BaseEntity<int>
    {
        public string UserId { get; set; } = default!;
        public string Token { get; set; } = default!;     // من Paymob
        public string? Last4 { get; set; }                // ****1234
        public string? Brand { get; set; }                // Visa/Mastercard
        public int? ExpiryMonth { get; set; }
        public int? ExpiryYear { get; set; }
        public string? PaymobTokenId { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
