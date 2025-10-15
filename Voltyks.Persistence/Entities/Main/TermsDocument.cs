using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main
{
    public class TermsDocument
    {
        public int Id { get; set; }
        public int VersionNumber { get; set; }    // 1,2,3,...
        public string Lang { get; set; } = "en";  // "en" | "ar"
        public bool IsActive { get; set; } = true;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public string PayloadJson { get; set; } = default!;
    }

}
