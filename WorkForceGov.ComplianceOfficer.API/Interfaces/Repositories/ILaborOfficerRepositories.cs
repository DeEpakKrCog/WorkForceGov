using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Repositories
{
    /// <summary>
    /// Data access for compliance records managed by Labor Officers.
    /// </summary>
    public interface IComplianceRecordRepository : IRepository<ComplianceRecord>
    {
        Task<IEnumerable<ComplianceRecord>> GetByEntityAsync(int entityId, string type);
        Task<IEnumerable<ComplianceRecord>> GetByOfficerAsync(int officerId);
        Task<IEnumerable<ComplianceRecord>> GetPendingReviewsAsync();
    }

    /// <summary>
    /// Data access for violation records issued against employers.
    /// </summary>
    public interface IViolationRepository : IRepository<Violation>
    {
        Task<IEnumerable<Violation>> GetByEmployerAsync(int employerId);
        Task<IEnumerable<Violation>> GetByOfficerAsync(int officerId);
    }

    /// <summary>
    /// Data access for system notifications.
    /// </summary>
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetByUserAsync(int userId, int count = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAllReadAsync(int userId);
    }
}
