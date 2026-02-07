using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChargingRequestEntity = Voltyks.Persistence.Entities.Main.ChargingRequest;

namespace Voltyks.Persistence.Entities.Main
{
    public enum ProcessStatus { PendingCompleted = 0, Completed = 1, Aborted = 2, Disputed = 3 }

    public class Process :BaseEntity<int>
    {        
        public int ChargerRequestId { get; set; }
        public string VehicleOwnerId { get; set; } = default!;
        public string ChargerOwnerId { get; set; } = default!;

        public decimal? EstimatedPrice { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? AmountCharged { get; set; }

        public ProcessStatus Status { get; set; } = ProcessStatus.PendingCompleted;

        /// <summary>
        /// Fine-grained sub-state for active processes (e.g., "awaiting_completion", "charging_in_progress").
        /// Null for final states (Completed, Aborted) or legacy records.
        /// </summary>
        public string? SubStatus { get; set; }

        public double? VehicleOwnerRating { get; set; }
        public double? ChargerOwnerRating { get; set; }

        /// <summary>
        /// UTC timestamp when the rating window was first opened (set once, idempotent).
        /// </summary>
        public DateTime? RatingWindowOpenedAt { get; set; }

        /// <summary>
        /// True if the background service applied default 3-star ratings after the window expired.
        /// </summary>
        public bool DefaultRatingApplied { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? DateCompleted { get; set; }

        // navs (اختياري)
        public ChargingRequestEntity? ChargerRequest { get; set; }
    }
}

