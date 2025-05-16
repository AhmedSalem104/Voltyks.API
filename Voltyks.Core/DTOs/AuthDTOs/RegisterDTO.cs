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
       // [Required(ErrorMessage = "UserName is required")]
       // [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "UserName can only contain letters and digits")]
        public string? UserName { get; set; }

        //[Required(ErrorMessage = "DisplayName is required")]
        public string? DisplayName { get; set; }

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

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        public string ConfirmPassword { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        public string? Street { get; set; }
        public string City { get; set; }
    }

}
