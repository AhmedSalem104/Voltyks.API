using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class Notification:BaseEntity<int>
    {
        public string? UserId { get; set; }
        public AppUser? User { get; set; }

        public string Title { get; set; }
        public string Body { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public int? UserTypeId { get; set; }
        public UserType? UserType { get; set; }

        public int? RelatedRequestId { get; set; }
        public ChargingRequest? RelatedRequest { get; set; }

        // Admin notification fields
        public string? Type { get; set; }
        public int? OriginalId { get; set; }
        public bool IsAdminNotification { get; set; } = false;
    }

}
