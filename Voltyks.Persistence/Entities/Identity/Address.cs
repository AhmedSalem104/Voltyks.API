using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Identity
{
    public class Address
    {
        public int Id { get; set; }

        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }


        // العلاقة بـ AppUser
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }

}
