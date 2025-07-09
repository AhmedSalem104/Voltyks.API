using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class Charger : BaseEntity<int>
    {
        public string UserId { get; set; }
        public AppUser User { get; set; }

        public int ProtocolId { get; set; }
        public Protocol Protocol { get; set; }

        public int CapacityId { get; set; }
        public Capacity Capacity { get; set; }

        public int PriceOptionId { get; set; }
        public PriceOption PriceOption { get; set; }

        public int AddressId { get; set; }
        public ChargerAddress Address { get; set; }

        public bool IsActive { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public bool? Adeptor { get; set; } 

    }

}
