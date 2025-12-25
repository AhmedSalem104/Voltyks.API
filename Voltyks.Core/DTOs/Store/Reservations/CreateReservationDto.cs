using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Store.Reservations
{
    public class CreateReservationDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; } = 1;
    }
}
