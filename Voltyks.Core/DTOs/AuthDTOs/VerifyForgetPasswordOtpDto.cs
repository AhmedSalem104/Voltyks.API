using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class VerifyForgetPasswordOtpDto
    {
        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 13 characters")]
        [RegularExpression(@"^(?:\+20|0020|0)?1[0125]\d{8}$", ErrorMessage = "Phone number Invalid Egyptian mobile")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "OTP code is required")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "OTP code must be exactly 4 digits")]
        public string OtpCode { get; set; }
    }

}
