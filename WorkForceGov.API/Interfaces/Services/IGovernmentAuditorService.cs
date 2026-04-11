using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    /// <summary>
    /// Business logic for the Government Auditor module.
    /// NO LONGER READ-ONLY — this module actively:
    ///   • Creates and conducts audit records
    ///   • Updates audit status (Open → In Progress → Completed)
    ///   • Records audit findings and recommendations
    ///   • Flags programs and employers for deeper investigation
    ///   • Generates audit reports with findings
    ///   • Reviews and validates compliance records
    ///
    /// Cross-module interaction:
    ///   • Reads compliance records created by ComplianceOfficer / LaborOfficer
    ///   • Reads employer data and program data
    ///   • Can escalate findings to create new compliance records
    /// </summary>
    public interface IGovernmentAuditorService
    {
        // ── Audit Management (ACTIVE) ──
        Task<IEnumerable<Audit>> GetAllAuditsAsync();
        Task<Audit?> GetAuditByIdAsync(int auditId);
        Task<(bool Success, string Message, Audit? Audit)> CreateAuditAsync(int officerId, string scope, string findings);

        /// <summary>
        /// Updates the status of an audit record (Open → In Progress → Completed).
        /// </summary>
        Task<(bool Success, string Message)> UpdateAuditStatusAsync(int auditId, string newStatus);

        /// <summary>
        /// Adds / updates findings to an existing audit.
        /// </summary>
        Task<(bool Success, string Message)> RecordAuditFindingsAsync(int auditId, string findings);

        // ── Compliance Review ──
        Task<IEnumerable<ComplianceRecord>> GetComplianceRecordsAsync();

        /// <summary>
        /// Escalates a compliance issue by creating a new compliance record
        /// and optionally flagging the entity for investigation.
        /// </summary>
        Task<(bool Success, string Message)> EscalateComplianceIssueAsync(
            int entityId, string entityType, int auditorId, string notes);

        // ── Program Review ──
        Task<IEnumerable<EmploymentProgram>> GetAllProgramsAsync();

        /// <summary>
        /// Flags a program for investigation and records the reason.
        /// </summary>
        Task<(bool Success, string Message)> FlagProgramForReviewAsync(int programId, int auditorId, string reason);

        // ── Report Generation ──
        Task<IEnumerable<Report>> GetAuditReportsAsync();
        Task<(bool Success, string Message, Report? Report)> GenerateAuditReportAsync(
            int auditorId, string reportName, string reportType, string content);

        // ── Employer Review ──
        Task<IEnumerable<Employer>> GetAllEmployersAsync();

        // ── Dashboard ──
        Task<AuditorDashboardViewModel> GetDashboardAsync(int userId);
    }
}
