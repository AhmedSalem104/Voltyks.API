using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Twilio.TwiML.Voice;
using Voltyks.Application.Interfaces;
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
            services.Configure<TwilioSettings>(configuration.GetSection("Twilio"));
            services.Configure<SmsEgyptSettings>(configuration.GetSection("SmsEgyptSettings"));
            services.Configure<SmsBeOnSettings>(configuration.GetSection("SmsBeOnSettings"));
            services.Configure<ChatApiSettings>(configuration.GetSection("ChatApi"));
            services.AddScoped<ITwilioService, TwilioService>();
            services.AddScoped<ISmsEgyptService, SmsEgyptService>();
            services.AddScoped<ISmsBeOnService, SmsBeOnService>();
            services.AddHttpClient();


            return services;

        }
     
    }
}
