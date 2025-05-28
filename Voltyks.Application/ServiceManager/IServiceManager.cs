using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Application.Interfaces;

namespace Voltyks.Application.ServicesManager.ServicesManager
{
    public interface IServiceManager
    {
        IAuthService AuthService { get; }
        ISmsEgyptService SmsEgyptService { get; }
        ISmsBeOnService SmsBeOnService { get; }

    }
}
