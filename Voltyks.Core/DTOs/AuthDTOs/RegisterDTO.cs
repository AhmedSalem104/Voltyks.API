using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    using System.ComponentModel.DataAnnotations;

    public class RegisterDTO
    {
        [Required(ErrorMessage = "FirstName is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "LastName is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^(\+2(010\d{8}|011\d{8}|012\d{8}|015\d{8})|010\d{8}|011\d{8}|012\d{8}|015\d{8})$", ErrorMessage = "Phone number must be in the format '010XXXXXXXX', '011XXXXXXXX', '012XXXXXXXX', '015XXXXXXXX' or '+2010XXXXXXXX', '+2011XXXXXXXX', '+2012XXXXXXXX', '+2015XXXXXXXX'")]

        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }
    }


}
