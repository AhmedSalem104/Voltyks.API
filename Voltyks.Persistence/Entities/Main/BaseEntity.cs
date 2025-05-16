using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main
{
    public class BaseEntity<TKey>
    {
        public TKey Id { get; set; }
    }
}
