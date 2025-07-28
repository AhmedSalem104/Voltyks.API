using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.ChargerRequest
{
    public class DeviceTokenDto
    {
        [Required]
        //[JsonPropertyName("deviceToken")] // إذا تستخدم System.Text.Json
        public string? DeviceToken { get; set; } 

    }
}
