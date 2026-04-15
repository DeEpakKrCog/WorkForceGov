using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.GovernmentAuditor
{
    /// <summary>
    /// Government Auditor Service — NO LONGER READ-ONLY.
    /// Actively creates audits, updates their status, records findings,
    /// escalates compliance issues, flags programs, and generates reports.
    /// </summary>
    public class GovernmentAuditorService : IGovernmentAuditorService
    {
        private readonly IAuditRepository _audits;
        private readonly IComplianceRecordRepository _compliance;
        private readonly IProgramRepository _programs;
        private readonly IReportRepository _reports;
        private readonly IEmployerRepository _employers;
        private readonly INotificationRepository _notifications;

        public GovernmentAuditorService(
            IAuditRepository audits,
            IComplianceRecordRepository compliance,
            IProgramRepository programs,
            IReportRepository reports,
            IEmployerRepository employers,
            INotificationRepository notifications)
        {
            _audits = audits;
            _compliance = compliance;
            _programs = programs;
            _reports = reports;
            _employers = employers;
            _notifications = notifications;
        }

        // ══════════════ Audit Management (ACTIVE) ══════════════

        public async Task<IEnumerable<Audit>> GetAllAuditsAsync() =>
            await _audits.GetAllAsync();

        public async Task<Audit?> GetAuditByIdAsync(int auditId) =>
            await _audits.GetByIdAsync(auditId);

        public async Task<(bool, string, Audit?)> CreateAuditAsync(
            int officerId, string scope, string findings)
        {
            var validScopes = new[] { "Employer", "Program", "Application", "System" };
            if (!validScopes.Contains(scope))
                return (false, $"Invalid scope. Must be one of: {string.Join(", ", validScopes)}", null);

            var audit = new Audit
            {
                OfficerId = officerId,
                Scope = scope,
                Findings = findings,
                Status = "Open",
                Date = DateTime.Now
            };

            await _audits.AddAsync(audit);
            await _audits.SaveAsync();
            return (true, "Audit created successfully.", audit);
        }

        public async Task<(bool, string)> UpdateAuditStatusAsync(int auditId, string newStatus)
        {
            var audit = await _audits.GetByIdAsync(auditId);
            if (audit == null) return (false, "Audit not found.");

            var validStatuses = new[] { "Open", "In Progress", "Completed", "Closed" };
            if (!validStatuses.Contains(newStatus))
                return (false, $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

            var oldStatus = audit.Status;
            audit.Status = newStatus;
            _audits.Update(audit);
            await _audits.SaveAsync();

            return (true, $"Audit status changed from '{oldStatus}' to '{newStatus}'.");
        }

        public async Task<(bool, string)> RecordAuditFindingsAsync(int auditId, string findings)
        {
            var audit = await _audits.GetByIdAsync(auditId);
            if (audit == null) return (false, "Audit not found.");

            audit.Findings = (audit.Findings ?? "") +
                $"\n\n--- Finding Added ({DateTime.Now:yyyy-MM-dd HH:mm}) ---\n{findings}";
            audit.Date = DateTime.Now;
            _audits.Update(audit);
            await _audits.SaveAsync();

            return (true, "Audit findings recorded.");
        }

        // ══════════════ Compliance Review ══════════════

        public async Task<IEnumerable<ComplianceRecord>> GetComplianceRecordsAsync() =>
            await _compliance.GetAllAsync();

        /// <summary>
        /// Escalates a compliance issue — creates a new "Under Review" compliance
        /// record that will be visible to Compliance Officers and Labor Officers.
        /// </summary>
        public async Task<(bool, string)> EscalateComplianceIssueAsync(
            int entityId, string entityType, int auditorId, string notes)
        {
            await _compliance.AddAsync(new ComplianceRecord
            {
                EntityId = entityId,
                Type = entityType,
                Result = "Under Review",
                Notes = $"[ESCALATED by Auditor #{auditorId}] {notes}",
                OfficerId = auditorId,
                Date = DateTime.Now
            });
            await _compliance.SaveAsync();

            return (true, $"Compliance issue escalated for {entityType} #{entityId}.");
        }

        // ══════════════ Program Review ══════════════

        public async Task<IEnumerable<EmploymentProgram>> GetAllProgramsAsync() =>
            await _programs.GetAllAsync();

        public async Task<(bool, string)> FlagProgramForReviewAsync(
            int programId, int auditorId, string reason)
        {
            var program = await _programs.GetByIdAsync(programId);
            if (program == null) return (false, "Program not found.");

            program.Status = "Under Audit";
            _programs.Update(program);

            // Create compliance record as audit trail
            await _compliance.AddAsync(new ComplianceRecord
            {
                EntityId = programId,
                Type = "Program",
                Result = "Under Review",
                Notes = $"Program flagged for audit review: {reason}",
                OfficerId = auditorId,
                Date = DateTime.Now
            });

            // Create an audit record
            await _audits.AddAsync(new Audit
            {
                OfficerId = auditorId,
                Scope = "Program",
                Findings = $"Program '{program.ProgramName}' flagged: {reason}",
                Status = "Open",
                Date = DateTime.Now
            });

            await _programs.SaveAsync();
            return (true, $"Program '{program.ProgramName}' flagged for audit review.");
        }

        // ══════════════ Report Generation ══════════════

        public async Task<IEnumerable<Report>> GetAuditReportsAsync() =>
            await _reports.FindAsync(r => r.ReportType == "Audit");

        public async Task<(bool, string, Report?)> GenerateAuditReportAsync(
            int auditorId, string reportName, string reportType, string content)
        {
            var report = new Report
            {
                ReportName = reportName,
                ReportType = reportType,
                ReportContent = content,
                GeneratedBy = auditorId,
                GeneratedDate = DateTime.Now
            };

            await _reports.AddAsync(report);
            await _reports.SaveAsync();
            return (true, "Audit report generated.", report);
        }

        // ══════════════ Employer Review ══════════════

        public async Task<IEnumerable<Models.Employer>> GetAllEmployersAsync() =>
            await _employers.GetAllAsync();

        // ══════════════ Dashboard ══════════════

        public async Task<AuditorDashboardViewModel> GetDashboardAsync(int userId)
        {
            var audits = (await _audits.GetAllAsync()).ToList();
            var compliance = (await _compliance.GetAllAsync()).ToList();
            var programs = (await _programs.GetAllAsync()).ToList();
            var notifications = (await _notifications.GetByUserAsync(userId, 10)).ToList();

            return new AuditorDashboardViewModel
            {
                TotalAudits = audits.Count,
                OpenAudits = audits.Count(a => a.Status == "Open"),
                CompletedAudits = audits.Count(a => a.Status == "Completed"),
                TotalCompliance = compliance.Count,
                NonCompliant = compliance.Count(c => c.Result == "Non-Compliant"),
                TotalPrograms = programs.Count,
                RecentAudits = audits.OrderByDescending(a => a.Date).Take(10).ToList(),
                RecentCompliance = compliance.OrderByDescending(c => c.Date).Take(10).ToList(),
                Programs = programs.Take(10).ToList(),
                Notifications = notifications
            };
        }
    }
}
