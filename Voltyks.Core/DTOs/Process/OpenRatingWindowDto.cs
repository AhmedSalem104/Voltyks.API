using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Process
{
    public class OpenRatingWindowDto
    {
        [Required(ErrorMessage = "Process ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ProcessId must be a positive integer")]
        public int ProcessId { get; set; }

        /// <summary>
        /// Optional client-side epoch timestamp (reference only — server time is authoritative).
        /// </summary>
        public long? ClientTimestamp { get; set; }
    }
}
