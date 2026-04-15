using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    /// <summary>
    /// Business logic for the Labor Officer module.
    /// Responsible for document verification, employer compliance monitoring,
    /// application review, and actioning complaints raised by citizens.
    /// 
    /// Cross-module consumption:
    ///   • Reads complaints created by CitizenService
    ///   • Reads employer documents uploaded by EmployerService
    ///   • Updates citizen document verification status
    /// </summary>
    public interface ILaborOfficerService
    {
        // ── Document Verification ──
        Task<IEnumerable<CitizenDocument>> GetPendingCitizenDocumentsAsync();
        Task<(bool Success, string Message)> VerifyCitizenDocumentAsync(int documentId, int officerId);
        Task<(bool Success, string Message)> RejectCitizenDocumentAsync(int documentId, int officerId, string reason);

        // ── Complaint Management (cross-module: reads from Citizen) ──
        /// <summary>
        /// Retrieves all complaints raised by citizens. The Labor Officer
        /// can then investigate and update their status.
        /// </summary>
        Task<IEnumerable<Complaint>> GetAllComplaintsAsync();
        Task<IEnumerable<Complaint>> GetPendingComplaintsAsync();
        Task<(bool Success, string Message)> InvestigateComplaintAsync(int complaintId, string resolution, string newStatus);

        // ── Employer Compliance ──
        Task<IEnumerable<ComplianceRecord>> GetComplianceRecordsAsync();
        Task<(bool Success, string Message)> CreateComplianceRecordAsync(ComplianceRecord record);

        // ── Violation Management ──
        Task<IEnumerable<Violation>> GetViolationsAsync();
        Task<(bool Success, string Message)> RecordViolationAsync(Violation violation);

        // ── Application Oversight ──
        Task<IEnumerable<Application>> GetAllApplicationsAsync();
        Task<(bool Success, string Message)> FlagApplicationAsync(int applicationId, string notes);

        // ── Dashboard ──
        Task<LaborOfficerDashboardViewModel> GetDashboardAsync(int userId);
    }
}
