using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class RecordDeliveryDto
    {
        [StringLength(1000)]
        public string? DeliveryNotes { get; set; }

        [StringLength(1000)]
        public string? AdminNotes { get; set; }
    }
}
