using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    /// <summary>
    /// Business logic for the Compliance Officer module.
    /// NO LONGER READ-ONLY — this module actively:
    ///   • Verifies / rejects employer documents (Verification Section)
    ///   • Updates employer status from "Pending" → "Verified" or "Suspended"
    ///   • Raises non-compliance flags on employers
    ///   • Investigates and resolves complaints
    ///   • Records violations with penalties
    ///
    /// Cross-module interaction:
    ///   • Reads employer documents uploaded by EmployerService
    ///   • Writes back verification status → triggers employer status change
    ///   • Reads complaints raised by CitizenService
    /// </summary>
    public interface IComplianceOfficerService
    {
        // ── Verification Section (cross-module: employer doc review) ──
        /// <summary>
        /// Retrieves all employer documents awaiting compliance review.
        /// </summary>
        Task<IEnumerable<EmployerDocument>> GetPendingEmployerDocumentsAsync();

        /// <summary>
        /// Approves an employer document. If ALL documents for the employer
        /// are now verified, the employer status is upgraded to "Verified".
        /// </summary>
        Task<(bool Success, string Message)> ApproveEmployerDocumentAsync(int documentId, int officerId);

        /// <summary>
        /// Rejects an employer document and flags the employer as "Suspended".
        /// </summary>
        Task<(bool Success, string Message)> RejectEmployerDocumentAsync(int documentId, int officerId, string reason);

        // ── Non-Compliance Flagging ──
        /// <summary>
        /// Flags an employer as non-compliant. Sets employer status to "Flagged"
        /// and creates a compliance record.
        /// </summary>
        Task<(bool Success, string Message)> FlagEmployerNonCompliantAsync(int employerId, int officerId, string reason);

        /// <summary>
        /// Clears a non-compliance flag after resolution.
        /// </summary>
        Task<(bool Success, string Message)> ClearNonComplianceFlagAsync(int employerId, int officerId, string notes);

        // ── Employer Review ──
        Task<IEnumerable<Employer>> GetAllEmployersAsync();
        Task<IEnumerable<Employer>> GetEmployersByStatusAsync(string status);
        Task<Employer?> GetEmployerDetailsAsync(int employerId);

        // ── Complaint Investigation ──
        Task<IEnumerable<Complaint>> GetAllComplaintsAsync();
        Task<(bool Success, string Message)> ResolveComplaintAsync(int complaintId, int officerId, string resolution);

        // ── Compliance Records ──
        Task<IEnumerable<ComplianceRecord>> GetAllComplianceRecordsAsync();
        Task<(bool Success, string Message)> CreateComplianceRecordAsync(ComplianceRecord record);
        Task<(bool Success, string Message)> UpdateComplianceResultAsync(int recordId, string result, string notes);

        // ── Violation Management ──
        Task<IEnumerable<Violation>> GetAllViolationsAsync();
        Task<(bool Success, string Message)> RecordViolationAsync(Violation violation);

        // ── Dashboard ──
        Task<ComplianceDashboardViewModel> GetDashboardAsync(int userId);
    }
}
