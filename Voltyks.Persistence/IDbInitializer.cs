using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence
{
    public interface IDbInitializer
    {
        Task InitializeAsync();
        Task InitializeIdentityAsync();
        Task ForceSeedAsync();
    }
}
