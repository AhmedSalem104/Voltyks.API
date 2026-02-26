using System.IO;
using System.IO.Compression;
using System.Data.Entity.Infrastructure;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
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
using Voltyks.Application.Services.ChargingRequest.Interceptor;
using Voltyks.Application.Interfaces.FeesConfig;
using Voltyks.Application.Services.FeesConfig;
using Voltyks.Application.Interfaces.Terms;
using Voltyks.Application.Services.Terms;
using Voltyks.AdminControlDashboard;
using Voltyks.Application.Interfaces.Caching;
using Voltyks.Application.Services.Caching;
using Voltyks.Application.Interfaces.Pagination;
using Voltyks.Application.Services.Pagination;
using Voltyks.API.Hubs;
using Voltyks.Application.Interfaces.SignalR;
using Voltyks.Application.Interfaces.ImageUpload;
using Voltyks.Application.Services.ImageUpload;
using Voltyks.API.Services;
using Voltyks.Application.Interfaces.AppSettings;
using Voltyks.Application.Services.AppSettings;
using Voltyks.Application.Services.Background;
using Voltyks.Application.Services.Background.Backup;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Core.DTOs.Processes;


namespace Voltyks.API.Extentions
{
    public static class Extentions
    {
        // Services
        public static IServiceCollection RegisterAllServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Response Compression for better performance
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    "application/json",
                    "text/json"
                });
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            // Output Caching for static endpoints
            services.AddOutputCache(options =>
            {
                options.DefaultExpirationTimeSpan = TimeSpan.Zero;

                options.AddPolicy("StaticData", policy =>
                {
                    policy.Expire(TimeSpan.FromMinutes(30));
                    policy.SetVaryByQuery("*");
                });

                options.AddPolicy("ShortCache", policy =>
                {
                    policy.Expire(TimeSpan.FromMinutes(5));
                    policy.SetVaryByQuery("*");
                });
            });

            services.AddBuildInServices();

            // Add CORS - Restricted to specific domains in production
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "https://voltyks.com", "https://www.voltyks.com", "https://admin.voltyks.com" };

            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecific", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });

                // SignalR CORS policy - requires credentials
                options.AddPolicy("SignalRPolicy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Rate Limiting for security (tuned for 10k concurrent users)
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Global rate limit: 1000 requests per minute per IP
                options.AddSlidingWindowLimiter("GlobalLimit", config =>
                {
                    config.PermitLimit = 1000;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.SegmentsPerWindow = 4;
                    config.QueueLimit = 10;
                    config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                });

                // Auth endpoints: 20 requests per minute per IP (prevent brute force)
                options.AddSlidingWindowLimiter("AuthLimit", config =>
                {
                    config.PermitLimit = 20;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.SegmentsPerWindow = 4;
                    config.QueueLimit = 5;
                    config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                });

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 1000,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 10,
                            QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
                        }));
            });

            // Add SignalR with Redis backplane for multi-instance scaling
            services.AddSignalR()
                .AddStackExchangeRedis(configuration.GetConnectionString("Redis")!, options =>
                {
                    options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("voltyks");
                });

            services.AddResponseCaching();
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
            services.AddHttpClient();
            services.AddHttpClient<PaymobService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.Configure<PaymobOptions>(configuration.GetSection("Paymob"));
            services.AddScoped<PaymobService>();

            // Database backup options
            services.Configure<DatabaseBackupOptions>(configuration.GetSection(DatabaseBackupOptions.SectionName));


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

            // Add Authorization service for role-based authorization
            services.AddAuthorization();

            // ✅ Firebase Admin Initialization
            if (FirebaseApp.DefaultInstance == null)
            {
                try
                {
                    var firebasePath = Path.Combine(AppContext.BaseDirectory, "Firebase", "service-account-key.json");
                    if (File.Exists(firebasePath))
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile(firebasePath)
                        });
                    }
                }
                catch { /* Skip Firebase if initialization fails */ }
            }

            // Add Interceptor
            services.AddScoped<ChargingRequestCleanupInterceptor>();

            // Single DbContext registration with interceptor + resilience
            services.AddDbContext<VoltyksDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
                options.AddInterceptors(sp.GetRequiredService<ChargingRequestCleanupInterceptor>());
            });

            return services;

        }


        private static IServiceCollection AddBuildInServices(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Enable proper Unicode/Arabic character encoding in JSON responses
                    options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                });
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

            // Security Headers Middleware
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
                await next();
            });

            // Swagger: always in Development, config-gated in Production (behind route key)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Voltyks API v1");
                    c.DefaultModelsExpandDepth(-1);
                });
            }
            else if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
            {
                var swaggerKey = app.Configuration["Swagger:RouteKey"];
                var hasKey = !string.IsNullOrWhiteSpace(swaggerKey);

                app.UseSwagger(c =>
                {
                    if (hasKey)
                        c.RouteTemplate = $"swagger/{{documentName}}/{swaggerKey}/swagger.json";
                });
                app.UseSwaggerUI(c =>
                {
                    var jsonPath = hasKey
                        ? $"/swagger/v1/{swaggerKey}/swagger.json"
                        : "/swagger/v1/swagger.json";
                    c.SwaggerEndpoint(jsonPath, "Voltyks API v1");
                    c.DefaultModelsExpandDepth(-1);
                    if (hasKey)
                        c.RoutePrefix = $"swagger/{swaggerKey}";
                });
            }

            app.UseStaticFiles();
            app.UseHttpsRedirection();

            // Response Compression - before routing for best effect
            app.UseResponseCompression();

            app.UseRouting();

            // Rate Limiting - after routing
            app.UseRateLimiter();

            // Output Caching - after routing, before auth
            app.UseOutputCache();

            // Enable CORS - restricted to specific domains
            app.UseCors("AllowSpecific");
            app.UseResponseCaching();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Map SignalR Hubs
            app.MapHub<ChargingRequestHub>("/hubs/charging-request").RequireCors("SignalRPolicy");
            app.MapHub<ProcessHub>("/hubs/process").RequireCors("SignalRPolicy");
            app.MapHub<NotificationHub>("/hubs/notification").RequireCors("SignalRPolicy");

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

            // إضافة AdminServiceManager
            services.AddScoped<IAdminServiceManager, AdminServiceManager>();

            // إضافة وحدة العمل (UnitOfWork)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IFeesConfigService, FeesConfigService>();
            services.AddScoped<ITermsService, TermsService>();

            // App Settings Service
            services.AddScoped<IAppSettingsService, AppSettingsService>();

            services.AddHttpClient("paymob"); // تقدر تضيف BaseAddress لو تحب
            services.AddSingleton<IPaymobAuthTokenProvider, PaymobAuthTokenProviderRedis>();


           

            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IPaymobService, PaymobService>();

            // CacheService
            services.AddScoped<ICacheService, CacheService>();

            // PaginationService
            services.AddScoped<IPaginationService, PaginationService>();

            // SignalR Service
            services.AddScoped<ISignalRService, SignalRService>();

            // Product Image Service
            services.AddScoped<IProductImageService, ProductImageService>();

            // Processes Service
            services.AddScoped<IProcessesService, ProcessesService>();

            // Background cleanup service for stale processes
            services.AddHostedService<StaleProcessCleanupService>();

            // Background service for rating window expiry (applies default 3★ after 5 min)
            services.AddHostedService<RatingWindowService>();

            // Daily database backup service
            services.AddHostedService<DatabaseBackupService>();

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
                    // SignalR: Read token from query string for WebSocket connections
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
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
                    ClockSkew = TimeSpan.FromMinutes(1) // Allow 1 min clock drift between servers
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
