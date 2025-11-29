using Voltyks.AdminControlDashboard.Interfaces;

namespace Voltyks.AdminControlDashboard
{
    public interface IAdminServiceManager
    {
        IAdminUsersService AdminUsersService { get; }
        IAdminFeesService AdminFeesService { get; }
        IAdminTermsService AdminTermsService { get; }
        IAdminProtocolService AdminProtocolService { get; }
        IAdminReportsService AdminReportsService { get; }
        IAdminBrandsService AdminBrandsService { get; }
        IAdminChargersService AdminChargersService { get; }
        IAdminVehiclesService AdminVehiclesService { get; }
        IAdminProcessService AdminProcessService { get; }
        IAdminComplaintCategoriesService AdminComplaintCategoriesService { get; }
    }
}
