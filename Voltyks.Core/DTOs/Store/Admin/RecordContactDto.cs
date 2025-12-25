using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class RecordContactDto
    {
        [StringLength(1000)]
        public string? AdminNotes { get; set; }
    }
}
