using Voltyks.AdminControlDashboard.Interfaces;
using Voltyks.AdminControlDashboard.Interfaces.Complaints;
using Voltyks.AdminControlDashboard.Interfaces.Notifications;

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
        IAdminComplaintsService AdminComplaintsService { get; }
        IAdminCapacityService AdminCapacityService { get; }
        IAdminNotificationsService AdminNotificationsService { get; }
    }
}
