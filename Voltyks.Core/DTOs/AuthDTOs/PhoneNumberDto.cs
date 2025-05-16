using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class PhoneNumberDto
    {
        [Phone]
        public string PhoneNumber { get; set; }
    }
}
