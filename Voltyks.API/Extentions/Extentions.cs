using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Voltyks.API.Middelwares;
using Voltyks.Application;
using Voltyks.Application.Services.AllowServices;
using Voltyks.Application.ServicesManager;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.ErrorModels;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.API.Extentions
{
    public static class Extentions
    {
        // Services
        public static IServiceCollection RegisterAllServices(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddBuildInServices();
            services.AddSwaggerServices();
            services.AddInfrastructureServices(configuration);
            services.AddIdentityServices();
            services.AddApplicationServices(configuration);
            services.ConfigreServices();
            services.ConfigureServices();
            services.ConfigureJwtServices(configuration);


            return services;

        }
        private static IServiceCollection AddBuildInServices(this IServiceCollection services)
        {
            services.AddControllers();
            return services;
        }
        private static IServiceCollection AddSwaggerServices(this IServiceCollection services)
        {

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;

        }
        private static IServiceCollection ConfigreServices(this IServiceCollection services)
        {


            services.Configure<ApiBehaviorOptions>(config =>
            {
                config.InvalidModelStateResponseFactory = (actionContext) =>
                {
                    var errors = actionContext.ModelState
                        .Where(m => m.Value.Errors.Any())
                        .Select(m => new ValidationError()
                        {
                            Field = m.Key,
                            Errors = m.Value.Errors.Select(errors => errors.ErrorMessage)
                        });

                    var response = new ValidationErrorResponse()
                    {
                        Errors = errors
                    };

                    return new BadRequestObjectResult(response);
                };
            });

            return services;

        }


        // MiddleWares
        public static async Task<WebApplication> ConfigurMiddleWares(this WebApplication app)
        {

            await app.InitializeDatabaseAsync();

            app.UseGlobalErrorHandling();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            return app;

        }
        private static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>(); // ASK CLR Create Object DbInitializer 
            await dbInitializer.InitializeAsync();
            await dbInitializer.InitializeIdentityAsync();

            return app;
        }
        private static WebApplication UseGlobalErrorHandling(this WebApplication app)
        {
            app.UseMiddleware<GlobalErrorHandlingMiddelwares>();
            return app;
        }
        private static IServiceCollection AddIdentityServices(this IServiceCollection services)
        {
            services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<VoltyksDbContext>();
            return services;
        }
        private static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();  // إضافة IHttpContextAccessor
            services.AddScoped<IAuthService, AuthService>(); // إضافة خدمة المصادقة
            services.AddScoped<IServiceManager, ServiceManager>(); // إضافة ServiceManager إذا كان مطلوبًا
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
        private static IServiceCollection ConfigureJwtServices(this IServiceCollection services, IConfiguration configuration)
        {

            var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>();

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(option =>
            {
                option.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)),
                };
            });

            return services;

        }
    }
}
