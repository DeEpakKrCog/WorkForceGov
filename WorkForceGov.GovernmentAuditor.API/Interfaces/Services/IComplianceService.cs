using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IComplianceService
    {
        Task<IEnumerable<ComplianceRecord>> GetAllComplianceRecordsAsync();
        Task<IEnumerable<ComplianceRecord>> GetAllAsync();
        Task<ComplianceRecord?> GetComplianceRecordByIdAsync(int id);
        Task<IEnumerable<ComplianceRecord>> GetComplianceRecordsByEntityAsync(int entityId, string type);
        Task<(bool Success, string Message)> CreateComplianceRecordAsync(ComplianceRecord record);
        Task<(bool Success, string Message)> UpdateComplianceRecordAsync(ComplianceRecord record);
        Task<object> GetDashboardAsync(int userId);
        Task CreateAsync(ComplianceRecord record);
    }
}
