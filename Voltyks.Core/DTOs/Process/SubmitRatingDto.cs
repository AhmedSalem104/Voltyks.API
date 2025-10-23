using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Process
{
    public class SubmitRatingDto
    {
        public int ProcessId { get; set; }
        public double RatingForOther { get; set; } // 1..5
    }
}
