using AutoMapper;
using Microsoft.AspNetCore.Http;
using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.AdminControlDashboard.Services;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Data;

namespace Voltyks.AdminControlDashboard
{
    public class AdminServiceManager : IAdminServiceManager
    {
        public AdminServiceManager(
            VoltyksDbContext context,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IServiceManager serviceManager)
        {
            AdminUsersService = new AdminUsersService(context, unitOfWork, mapper, httpContextAccessor);
            AdminFeesService = new AdminFeesService(serviceManager.FeesConfigService, context);
            AdminTermsService = new AdminTermsService(serviceManager.TermsService, context);
            AdminProtocolService = new AdminProtocolService(context);
            AdminReportsService = new AdminReportsService(context, mapper, httpContextAccessor);
            AdminBrandsService = new AdminBrandsService(context);
            AdminChargersService = new AdminChargersService(context);
            AdminVehiclesService = new AdminVehiclesService(context);
            AdminProcessService = new AdminProcessService(context);
            AdminComplaintCategoriesService = new AdminComplaintCategoriesService(context);
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
    }
}
