using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.intention
{
    public record ItemDto(string Name, int Amount, string? Description = null, int Quantity = 1);

}
