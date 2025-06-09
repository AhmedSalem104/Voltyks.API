using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Twilio.TwiML.Voice;
using Voltyks.Application.Interfaces.SMSEgypt;
using Voltyks.Application.Services.SMSEgypt;
using Voltyks.Application.ServicesManager;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Persistence.Entities.Main;


namespace Voltyks.Application.Services.AllowServices
{
    public static class ApplicationServicesRegisteration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services,IConfiguration configuration) {

            services.AddAutoMapper(typeof(AssemblyReference).Assembly);
            services.AddScoped<IServiceManager, ServiceManager>();
            services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));
            services.Configure<SmsEgyptSettings>(configuration.GetSection("SmsEgyptSettings"));
            services.AddScoped<ISmsEgyptService, SmsEgyptService>();
            services.AddHttpClient();


            return services;

        }
     
    }
}
