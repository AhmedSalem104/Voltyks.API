using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Terms
{
    public class TermsResponseDto
    {
        public int Version { get; set; }
        public string Lang { get; set; } = "en";
        public DateTime PublishedAt { get; set; }
        public object Content { get; set; } = default!;
    }
}
