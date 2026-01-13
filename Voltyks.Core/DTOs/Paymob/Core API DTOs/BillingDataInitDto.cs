using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Core_API_DTOs
{
    /// <summary>
    /// Billing data DTO - supports both camelCase (iOS) and snake_case property names
    /// </summary>
    public class BillingDataInitDto
    {
        [JsonPropertyName("first_name")]
        public string first_name { get; set; } = "NA";

        [JsonPropertyName("last_name")]
        public string last_name { get; set; } = "NA";

        [JsonPropertyName("email")]
        public string email { get; set; } = "na@example.com";

        [JsonPropertyName("phone_number")]
        public string phone_number { get; set; } = "00000000000";

        // Accept camelCase from iOS app
        [JsonPropertyName("firstName")]
        public string? FirstName { set => first_name = value ?? first_name; }

        [JsonPropertyName("lastName")]
        public string? LastName { set => last_name = value ?? last_name; }

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { set => phone_number = value ?? phone_number; }
    }
}

