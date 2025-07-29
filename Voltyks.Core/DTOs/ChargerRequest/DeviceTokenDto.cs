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
        public string? DeviceToken { get; set; }
        //public string? RoleContext { get; set; }  // "VehicleOwner" or "ChargerOwner"


    }
}
