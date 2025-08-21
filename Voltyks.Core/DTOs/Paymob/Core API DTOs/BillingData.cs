using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Core_API_DTOs
{
    public record BillingData(
      string first_name, string last_name, string email, string phone_number,
      string apartment = "NA", string floor = "NA", string street = "NA",
      string building = "NA", string shipping_method = "NA", string postal_code = "NA",
      string city = "NA", string country = "EG", string state = "NA");
}
