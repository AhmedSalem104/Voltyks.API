using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.AdminControlDashboard.Interfaces.Complaints;
using Voltyks.AdminControlDashboard.Interfaces.Notifications;
using Voltyks.AdminControlDashboard.Services;
using Voltyks.AdminControlDashboard.Services.Complaints;
using Voltyks.AdminControlDashboard.Services.Notifications;
using Voltyks.Application.Interfaces.Redis;
using Voltyks.Application.Interfaces.SMSEgypt;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.AdminControlDashboard
{
    public class AdminServiceManager : IAdminServiceManager
    {
        public AdminServiceManager(
            VoltyksDbContext context,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IServiceManager serviceManager,
            UserManager<AppUser> userManager,
            ISmsEgyptService smsEgyptService,
            IRedisService redisService)
        {
            AdminUsersService = new AdminUsersService(context, unitOfWork, mapper, httpContextAccessor, userManager, smsEgyptService, redisService);
            AdminFeesService = new AdminFeesService(serviceManager.FeesConfigService, context, httpContextAccessor);
            AdminTermsService = new AdminTermsService(serviceManager.TermsService, context);
            AdminProtocolService = new AdminProtocolService(context);
            AdminReportsService = new AdminReportsService(context, mapper, httpContextAccessor);
            AdminBrandsService = new AdminBrandsService(context);
            AdminChargersService = new AdminChargersService(context);
            AdminVehiclesService = new AdminVehiclesService(context);
            AdminProcessService = new AdminProcessService(context);
            AdminComplaintCategoriesService = new AdminComplaintCategoriesService(context);
            AdminComplaintsService = new AdminComplaintsService(context);
            AdminCapacityService = new AdminCapacityService(context);
            AdminNotificationsService = new AdminNotificationsService(context);
            AdminStoreService = new AdminStoreService(context);
        }

        public IAdminUsersService AdminUsersService { get; }
        public IAdminFeesService AdminFeesService { get; }
        public IAdminTermsService AdminTermsService { get; }
        public IAdminProtocolService AdminProtocolService { get; }
        public IAdminReportsService AdminReportsService { get; }
        public IAdminBrandsService AdminBrandsService { get; }
        public IAdminChargersService AdminChargersService { get; }
        public IAdminVehiclesService AdminVehiclesService { get; }
        public IAdminProcessService AdminProcessService { get; }
        public IAdminComplaintCategoriesService AdminComplaintCategoriesService { get; }
        public IAdminComplaintsService AdminComplaintsService { get; }
        public IAdminCapacityService AdminCapacityService { get; }
        public IAdminNotificationsService AdminNotificationsService { get; }
        public IAdminStoreService AdminStoreService { get; }
    }
}
