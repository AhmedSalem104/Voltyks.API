using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class Model : BaseEntity<int>
    {
        public string Name { get; set; }

        // Foreign Keys
        public int BrandId { get; set; }

        // Navigation properties
        public Brand? Brand { get; set; }


    }

}
