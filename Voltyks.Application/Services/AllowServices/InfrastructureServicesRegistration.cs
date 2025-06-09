using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Voltyks.Application.Interfaces.Redis;
using Voltyks.Application.Services.Redis;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence;
using Voltyks.Persistence.Data;


namespace Voltyks.Application.Services.AllowServices
{
    public static class InfrastructureServicesRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services ,IConfiguration   configuration) 
        {


            services.AddDbContext<VoltyksDbContext>(options =>
            {
                //options.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]);
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            //services.AddDbContext<IdentityVoltyksDbContext>(options =>
            //{
            //    options.UseSqlServer(configuration.GetConnectionString("IdentityConnection"));
            //});

            services.AddScoped<IDbInitializer, DbInitializer>(); // Allow DI For DbInitializer


            services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
            {
              return ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!);
            }            
            );

            services.AddSingleton<IRedisService, RedisService>();

            return services;
        }
    }
}
