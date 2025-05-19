using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Voltyks.Persistence.Entities.Identity
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; } 
        public string? NationalId { get; set; }
        public string? RefreshToken { get; set; }
        public int? Vcode { get; set; }
        public DateTime? VcodeExpirationDate { get; set; }

        public Address Address { get; set; }

    }
}
