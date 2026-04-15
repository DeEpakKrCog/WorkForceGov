using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.GovernmentAuditor
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;
        private readonly IComplianceRecordRepository _compliance;
        private readonly IProgramRepository _programs;
        private readonly INotificationRepository _notifications;

        public AuditService(
            IAuditRepository auditRepository,
            IComplianceRecordRepository compliance,
            IProgramRepository programs,
            INotificationRepository notifications)
        {
            _auditRepository = auditRepository;
            _compliance = compliance;
            _programs = programs;
            _notifications = notifications;
        }

        public async Task<IEnumerable<Audit>> GetAllAuditsAsync() =>
            await _auditRepository.GetAllAsync();

        public async Task<IEnumerable<Audit>> GetAllAsync() =>
            await _auditRepository.GetAllAsync();

        public async Task<Audit?> GetAuditByIdAsync(int id) =>
            await _auditRepository.GetByIdAsync(id);

        public async Task<IEnumerable<Audit>> GetAuditsByOfficerAsync(int officerId)
        {
            var audits = await _auditRepository.GetAllAsync();
            return audits.Where(a => a.OfficerId == officerId).ToList();
        }

        public async Task<(bool Success, string Message)> CreateAuditAsync(Audit audit)
        {
            try
            {
                await _auditRepository.AddAsync(audit);
                await _auditRepository.SaveAsync();
                return (true, "Audit created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating audit: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAuditAsync(Audit audit)
        {
            try
            {
                _auditRepository.Update(audit);
                await _auditRepository.SaveAsync();
                return (true, "Audit updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating audit: {ex.Message}");
            }
        }

        public async Task<AuditorDashboardViewModel> GetDashboardAsync(int userId)
        {
            var audits = (await _auditRepository.GetAllAsync()).ToList();
            var complianceRecords = (await _compliance.GetAllAsync()).ToList();
            var programs = (await _programs.GetAllAsync()).ToList();
            var notifications = (await _notifications.GetByUserAsync(userId, 10)).ToList();

            return new AuditorDashboardViewModel
            {
                TotalAudits = audits.Count,
                OpenAudits = audits.Count(a => a.Status == "Open" || a.Status == "In Progress"),
                CompletedAudits = audits.Count(a => a.Status == "Completed"),
                TotalCompliance = complianceRecords.Count,
                NonCompliant = complianceRecords.Count(c => c.Result == "Non-Compliant"),
                TotalPrograms = programs.Count,
                RecentAudits = audits.OrderByDescending(a => a.Date).Take(10).ToList(),
                RecentCompliance = complianceRecords.OrderByDescending(c => c.Date).Take(10).ToList(),
                Programs = programs.Take(10).ToList(),
                Notifications = notifications
            };
        }

        public async Task CreateAsync(Audit audit)
        {
            await _auditRepository.AddAsync(audit);
            await _auditRepository.SaveAsync();
        }
    }
}
