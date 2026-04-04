using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardViewModel> GetDashboardAsync();
    }
}
