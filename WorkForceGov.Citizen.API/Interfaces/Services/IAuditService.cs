using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IAuditService
    {
        Task<IEnumerable<Audit>> GetAllAuditsAsync();
        Task<IEnumerable<Audit>> GetAllAsync();
        Task<Audit?> GetAuditByIdAsync(int id);
        Task<IEnumerable<Audit>> GetAuditsByOfficerAsync(int officerId);
        Task<(bool Success, string Message)> CreateAuditAsync(Audit audit);
        Task<(bool Success, string Message)> UpdateAuditAsync(Audit audit);
        Task<AuditorDashboardViewModel> GetDashboardAsync(int userId);
        Task CreateAsync(Audit audit);
    }
}
