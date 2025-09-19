using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.CardsDTOs
{
    public record SavedCardViewDto(int Id, string? Brand, string? Last4, int? ExpiryMonth, int? ExpiryYear, bool IsDefault);

}
