using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class Vehicle : BaseEntity<int>
    {
        public string Color { get; set; } 
        public string Plate { get; set; } 
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public int Year { get; set; }
        public bool IsDeleted { get; set; } = false; 


        // Foreign Keys
        public int BrandId { get; set; } 
        public int ModelId { get; set; } 
        public string UserId { get; set; }  

        // Navigation properties
        public Brand? Brand { get; set; }
        public Model? Model { get; set; }
        public AppUser? User { get; set; } 
    }


}
