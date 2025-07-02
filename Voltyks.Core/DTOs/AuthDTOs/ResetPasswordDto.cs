using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(13, MinimumLength = 11, ErrorMessage = "Phone number must be 11 or 13 characters")]
        [RegularExpression(@"^(\+2(010\d{8}|011\d{8}|012\d{8}|015\d{8})|010\d{8}|011\d{8}|012\d{8}|015\d{8})$", ErrorMessage = "Phone number must be in the format '010XXXXXXXX', '011XXXXXXXX', '012XXXXXXXX', '015XXXXXXXX' or '+2010XXXXXXXX', '+2011XXXXXXXX', '+2012XXXXXXXX', '+2015XXXXXXXX'")]

        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string NewPassword { get; set; }
    }
}
