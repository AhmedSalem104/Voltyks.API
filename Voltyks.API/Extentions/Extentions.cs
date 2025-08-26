using System.Data.Entity.Infrastructure;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Voltyks.API.Middelwares;
using Voltyks.Application.Interfaces.Auth;
using Voltyks.Application.Services.AllowServices;
using Voltyks.Application.Services.Auth;
using Voltyks.Application.ServicesManager;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.ErrorModels;
using Voltyks.Core.Mapping;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Voltyks.Application.Interfaces.Brand;
using Voltyks.Application.Services;
using Voltyks.Application.Interfaces;
using Voltyks.Application.Services.Paymob;
using Voltyks.Core.DTOs.Paymob.Options;
using Voltyks.Application.Interfaces.Paymob;


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
            services.Configure<SmsEgyptSettings>(configuration.GetSection("SmsSettings"));
            services.AddSingleton<SqlConnectionFactory>();
            services.AddAutoMapper(typeof(MappingProfile).Assembly);
            services.AddHttpClient<PaymobService>();
            services.Configure<PaymobOptions>(configuration.GetSection("Paymob"));
            services.AddScoped<PaymobService>();


            services.AddAuthentication()
            .AddGoogle("Google", options =>
            {
                options.ClientId = configuration["Authentication:Google:client_id"];
                options.ClientSecret = configuration["Authentication:Google:client_secret"];
            })
            .AddFacebook("Facebook", options =>
            {
                options.AppId = configuration["Authentication:Facebook:client_id"];
                options.AppSecret = configuration["Authentication:Facebook:client_secret"];
            });
            services.AddDbContext<VoltyksDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // ✅ Firebase Admin Initialization
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("Firebase/service-account-key.json")
                });
            }
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
                
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Voltyks API v1");
                    c.DefaultModelsExpandDepth(-1); // ✅ إخفاء قسم Schemas
                });
            }
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseRouting();
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


            // تسجيل IBrandService مع BrandService
            services.AddScoped<IBrandService, BrandService>();

            // إضافة IHttpContextAccessor
            services.AddHttpContextAccessor();

            // إضافة خدمة المصادقة
            services.AddScoped<IAuthService, AuthService>();

            // إضافة ServiceManager إذا كان مطلوبًا
            services.AddScoped<IServiceManager, ServiceManager>();

            // إضافة وحدة العمل (UnitOfWork)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddHttpClient("paymob"); // تقدر تضيف BaseAddress لو تحب
            services.AddSingleton<IPaymobAuthTokenProvider, PaymobAuthTokenProviderRedis>();

            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IPaymobService, PaymobService>();


            return services;


            
        }
        private static IServiceCollection ConfigureJwtServices(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // ✅ تعديل الرسالة هنا
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var response = new ApiResponse<string>(
                            message: "Authorization token is missing or invalid.",
                            status: false
                        );

                        var result = JsonSerializer.Serialize(response);
                        return context.Response.WriteAsync(result);
                    }
                };

                options.RequireHttpsMetadata = false; // useful in development
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)),
                    ClockSkew = TimeSpan.Zero // ⬅️ optional: to reduce token expiry delays
                };
            });

            // ✅ Add support for JWT in Swagger (inside same method optionally)
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter 'Bearer' followed by space and your JWT token."
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
            });

            return services;
        }

        //private static IServiceCollection ConfigureJwtServices(this IServiceCollection services, IConfiguration configuration)
        //{

        //    var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>();

        //    services.AddAuthentication(option =>
        //    {
        //        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //    }).AddJwtBearer(option =>
        //    {
        //        option.TokenValidationParameters = new TokenValidationParameters()
        //        {
        //            ValidateIssuer = true,
        //            ValidateAudience = true,
        //            ValidateIssuerSigningKey = true,
        //            ValidateLifetime = true,

        //            ValidIssuer = jwtOptions.Issuer,
        //            ValidAudience = jwtOptions.Audience,
        //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)),
        //        };
        //    });

        //    return services;

        //}
    }
}
