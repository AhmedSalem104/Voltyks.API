using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.Core.DTOs.Process
{
    public class SubmitRatingDto : ILocalizedRequest
    {
        [Required(ErrorMessage = "Process ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ProcessId must be a positive integer")]
        public int ProcessId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1.0, 5.0, ErrorMessage = "Rating must be between 1 and 5")]
        public double RatingForOther { get; set; } // 1..5

        /// <summary>Optional language for the notification ("en"/"ar"). Defaults to "en".</summary>
        public string? Lang { get; set; }
    }
}
