using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main.Paymob
{
    // ProcessedWebhook.cs
    public class ProcessedWebhook : BaseEntity<int>
    {
        public int Id { get; set; }
        public string Type { get; set; } = default!;           
        public string ProviderEventId { get; set; } = default!; 
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

}
