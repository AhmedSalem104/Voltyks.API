using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Application.Interfaces;
using Voltyks.Application.Interfaces.Auth;
using Voltyks.Application.Interfaces.Brand;
using Voltyks.Application.Interfaces.SMSEgypt;

namespace Voltyks.Application.ServicesManager.ServicesManager
{
    public interface IServiceManager
    {
        IAuthService AuthService { get; }
        ISmsEgyptService SmsEgyptService { get; }
        IBrandService BrandService { get; }
        IModelService ModelService { get; }
        IVehicleService VehicleService  { get; }


    }
}
