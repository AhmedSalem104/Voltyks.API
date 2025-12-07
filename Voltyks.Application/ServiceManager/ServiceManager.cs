
using System.Net.Http;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces;
using Voltyks.Application.Interfaces.Auth;
using Voltyks.Application.Interfaces.Brand;
using Voltyks.Application.Interfaces.ChargerStation;
using Voltyks.Application.Interfaces.ChargingRequest;
using Voltyks.Application.Interfaces.FeesConfig;
using Voltyks.Application.Interfaces.Firebase;
using Voltyks.Application.Interfaces.Paymob;
using Voltyks.Application.Interfaces.Processes;
using Voltyks.Application.Interfaces.Pagination;
using Voltyks.Application.Interfaces.Redis;
using Voltyks.Application.Interfaces.SMSEgypt;
using Voltyks.Application.Interfaces.Terms;
using Voltyks.Application.Interfaces.UserReport;
using Voltyks.Application.Services;
using Voltyks.Application.Services.Auth;
using Voltyks.Application.Services.ChargingRequest;
using Voltyks.Application.Services.FeesConfig;
using Voltyks.Application.Services.Paymob;
using Voltyks.Application.Services.SMSEgypt;
using Voltyks.Application.Services.Terms;
using Voltyks.Application.Services.UserReport;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Core.DTOs.Paymob.Options;
using Voltyks.Core.DTOs.Processes;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.ServicesManager
{
    public class ServiceManager(UserManager<AppUser> userManager,
        IHttpContextAccessor httpContextAccessor
        ,IHttpClientFactory httpClientFactory
        , IOptions<JwtOptions> options
        , IOptions<SmsEgyptSettings> SmsSettings
        , IRedisService redisService
        , IConfiguration configuration
        , IUnitOfWork unitOfWork
        , VoltyksDbContext context
        , IMapper mapper
        , IFirebaseService firebaseService
        , IVehicleService vehicleService
        , HttpClient _http
        ,IOptions<PaymobOptions> _opt
        ,ILogger<PaymobService> _log
        , IPaymobAuthTokenProvider tokenProvider,
    IFeesConfigService feesConfigService,
    ILogger<ProcessesService> processesLogger,
    IPaginationService paginationService) : IServiceManager
    {
      
        public IAuthService AuthService { get; } = new AuthService(userManager, httpContextAccessor, options, redisService,configuration, mapper, unitOfWork, context, vehicleService);
        public ISmsEgyptService SmsEgyptService { get; } = new SmsEgyptService(redisService, httpClientFactory, SmsSettings, userManager);
        public IBrandService BrandService { get; } = new BrandService(unitOfWork);
        public IModelService ModelService  { get; } = new ModelService(unitOfWork, mapper);
        public IVehicleService VehicleService { get; } = new VehicleService(unitOfWork, mapper , httpContextAccessor);
        public IChargerService ChargerService { get; } = new ChargerService(unitOfWork, mapper, httpContextAccessor);
        public IChargingRequestService ChargingRequestService { get; } = new ChargingRequestService(unitOfWork, firebaseService, httpContextAccessor, vehicleService, feesConfigService,context, httpClientFactory);
        public IPaymobService PaymobService { get; } = new PaymobService(_http, _opt, unitOfWork, _log, tokenProvider, httpContextAccessor, httpClientFactory, userManager);
        public IFeesConfigService FeesConfigService { get; } = new FeesConfigService(unitOfWork, mapper, httpContextAccessor);
        public ITermsService TermsService { get; } = new TermsService(context);
        public IProcessesService ProcessesService  { get; } = new ProcessesService(context, httpContextAccessor, firebaseService, processesLogger, redisService, paginationService);
        public IUserReportService UserReportService  { get; } = new UserReportService(context,mapper,unitOfWork, httpContextAccessor,firebaseService);





    }
}
