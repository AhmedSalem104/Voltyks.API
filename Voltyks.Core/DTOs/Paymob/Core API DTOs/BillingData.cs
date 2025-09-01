using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Core_API_DTOs
{
    public record BillingData(
        string first_name,
        string last_name,
        string email,
        string phone_number,
        string city,
        string country
    );

}
