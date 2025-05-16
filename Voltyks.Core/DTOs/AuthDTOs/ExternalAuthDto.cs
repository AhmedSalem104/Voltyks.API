using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class ExternalAuthDto
    {
        public string Provider { get; set; } // "google" or "facebook"
        public string IdToken { get; set; } // أو AccessToken من Facebook
    }

}
