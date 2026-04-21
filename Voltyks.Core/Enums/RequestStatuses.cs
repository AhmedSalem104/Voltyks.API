using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.Enums
{
    public static class RequestStatuses
    {
        public const string Pending = "pending";
        public const string Accepted = "accepted";
        public const string Rejected = "rejected";
        public const string Confirmed = "confirmed";
        public const string Aborted = "aborted";

        // Post-session statuses (previously hardcoded PascalCase throughout the app).
        // Kept lowercase for in-memory equality consistency with the constants above.
        public const string Completed = "completed";
        public const string Started = "started";
        public const string Expired = "expired";

        // Interim state before both parties confirm. Stored value kept PascalCase to
        // avoid breaking existing records; future cleanup could standardize.
        public const string PendingCompleted = "PendingCompleted";
    }

}
