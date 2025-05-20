using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.AuthDTOs;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Application.ServicesManager
{
    public class ServiceManager(UserManager<AppUser> userManager,
        IHttpContextAccessor httpContextAccessor
        , IOptions<JwtOptions> options
        , IOptions<TwilioSettings> twilioSettings
        , IRedisService redisService
        , IConfiguration configuration) : IServiceManager
    {
      
        public IAuthService AuthService { get; } = new AuthService(userManager, httpContextAccessor, options, twilioSettings, redisService,configuration);


    }
}
