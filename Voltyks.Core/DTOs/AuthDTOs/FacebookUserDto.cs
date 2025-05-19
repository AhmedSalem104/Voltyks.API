using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class FacebookUserDto
    {
       
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
    }

}
