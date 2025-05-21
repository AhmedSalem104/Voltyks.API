using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.TwilioConfDTOs
{
    public class VerifyOtpDto
    {
        public string PhoneNumber { get; set; }
        public string Code { get; set; }
    }
}
