using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public record CreateServiceOrderDto(long AmountCents, string Currency = "EGP", int UserId = 0);

}
