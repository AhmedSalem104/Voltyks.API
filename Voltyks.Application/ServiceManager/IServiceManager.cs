using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Application.ServicesManager.ServicesManager
{
    public interface IServiceManager
    {
        IAuthService AuthService { get; }
    }
}
