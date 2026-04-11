using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.ComplianceOfficer
{
    /// <summary>
    /// Compliance Officer Service — NO LONGER READ-ONLY.
    /// 
    /// Cross-module interaction flow:
    ///   1. Employer uploads document → EmployerService sets employer.Status = "Pending"
    ///   2. Compliance Officer reviews → this service approves/rejects documents
    ///   3. If ALL docs approved → employer.Status becomes "Verified"
    ///   4. If any doc rejected → employer.Status becomes "Suspended"
    ///   5. Non-compliance flags → employer.Status becomes "Flagged"
    /// </summary>
    public class ComplianceOfficerService : IComplianceOfficerService
    {
        private readonly IEmployerDocumentRepository _employerDocs;
        private readonly IEmployerRepository _employers;
        private readonly IComplaintRepository _complaints;
        private readonly IComplianceRecordRepository _compliance;
        private readonly IViolationRepository _violations;
        private readonly INotificationRepository _notifications;

        public ComplianceOfficerService(
            IEmployerDocumentRepository employerDocs,
            IEmployerRepository employers,
            IComplaintRepository complaints,
            IComplianceRecordRepository compliance,
            IViolationRepository violations,
            INotificationRepository notifications)
        {
            _employerDocs = employerDocs;
            _employers = employers;
            _complaints = complaints;
            _compliance = compliance;
            _violations = violations;
            _notifications = notifications;
        }

        // ══════════════════════════════════════════════════════
        //  VERIFICATION SECTION (cross-module: reads from Employer)
        // ══════════════════════════════════════════════════════

        public async Task<IEnumerable<EmployerDocument>> GetPendingEmployerDocumentsAsync() =>
            await _employerDocs.GetPendingVerificationsAsync();

        /// <summary>
        /// Approves an employer document. After approval, checks whether ALL documents
        /// for this employer are now verified. If yes, upgrades employer status to "Verified".
        /// </summary>
        public async Task<(bool, string)> ApproveEmployerDocumentAsync(int documentId, int officerId)
        {
            var doc = await _employerDocs.GetByIdAsync(documentId);
            if (doc == null) return (false, "Document not found.");

            doc.VerificationStatus = "Verified";
            _employerDocs.Update(doc);
            await _employerDocs.SaveAsync();

            // ── Check if ALL employer documents are now verified ──
            var allDocs = (await _employerDocs.GetByEmployerAsync(doc.EmployerId)).ToList();
            bool allVerified = allDocs.All(d => d.VerificationStatus == "Verified");

            if (allVerified)
            {
                var employer = await _employers.GetByIdAsync(doc.EmployerId);
                if (employer != null)
                {
                    employer.Status = "Verified";
                    _employers.Update(employer);
                    await _employers.SaveAsync();

                    // Notify employer
                    await _notifications.AddAsync(new Notification
                    {
                        UserId = employer.UserId,
                        Message = "All documents verified. Your employer status is now 'Verified'. You can post jobs.",
                        Category = "ComplianceVerification",
                        EntityId = employer.Id,
                        EntityType = "Employer"
                    });
                    await _notifications.SaveAsync();
                }
            }

            // Create compliance record for audit trail
            await _compliance.AddAsync(new ComplianceRecord
            {
                EntityId = doc.EmployerId,
                Type = "Employer",
                Result = allVerified ? "Compliant" : "Under Review",
                Notes = $"Document '{doc.DocType}' approved by officer #{officerId}.",
                OfficerId = officerId,
                Date = DateTime.Now
            });
            await _compliance.SaveAsync();

            return (true, allVerified
                ? "Document approved. All documents verified — employer status upgraded to 'Verified'."
                : "Document approved. Other documents still pending review.");
        }

        /// <summary>
        /// Rejects an employer document and sets employer status to "Suspended".
        /// </summary>
        public async Task<(bool, string)> RejectEmployerDocumentAsync(
            int documentId, int officerId, string reason)
        {
            var doc = await _employerDocs.GetByIdAsync(documentId);
            if (doc == null) return (false, "Document not found.");

            doc.VerificationStatus = "Rejected";
            _employerDocs.Update(doc);

            // Suspend the employer
            var employer = await _employers.GetByIdAsync(doc.EmployerId);
            if (employer != null)
            {
                employer.Status = "Suspended";
                _employers.Update(employer);

                await _notifications.AddAsync(new Notification
                {
                    UserId = employer.UserId,
                    Message = $"Document '{doc.DocType}' rejected: {reason}. Employer status suspended.",
                    Category = "ComplianceRejection",
                    EntityId = employer.Id,
                    EntityType = "Employer"
                });
            }

            await _compliance.AddAsync(new ComplianceRecord
            {
                EntityId = doc.EmployerId,
                Type = "Employer",
                Result = "Non-Compliant",
                Notes = $"Document '{doc.DocType}' rejected. Reason: {reason}",
                OfficerId = officerId,
                Date = DateTime.Now
            });

            await _employerDocs.SaveAsync();
            return (true, "Document rejected. Employer status set to 'Suspended'.");
        }

        // ══════════════════════════════════════════════════════
        //  NON-COMPLIANCE FLAGGING (ACTIVE write operations)
        // ══════════════════════════════════════════════════════

        public async Task<(bool, string)> FlagEmployerNonCompliantAsync(
            int employerId, int officerId, string reason)
        {
            var employer = await _employers.GetByIdAsync(employerId);
            if (employer == null) return (false, "Employer not found.");

            employer.Status = "Flagged";
            _employers.Update(employer);

            await _compliance.AddAsync(new ComplianceRecord
            {
                EntityId = employerId,
                Type = "Employer",
                Result = "Non-Compliant",
                Notes = $"Flagged for non-compliance: {reason}",
                OfficerId = officerId,
                Date = DateTime.Now
            });

            await _notifications.AddAsync(new Notification
            {
                UserId = employer.UserId,
                Message = $"Your company has been flagged for non-compliance: {reason}",
                Category = "NonCompliance",
                EntityId = employerId,
                EntityType = "Employer"
            });

            await _employers.SaveAsync();
            return (true, "Employer flagged as non-compliant.");
        }

        public async Task<(bool, string)> ClearNonComplianceFlagAsync(
            int employerId, int officerId, string notes)
        {
            var employer = await _employers.GetByIdAsync(employerId);
            if (employer == null) return (false, "Employer not found.");

            employer.Status = "Verified";
            _employers.Update(employer);

            await _compliance.AddAsync(new ComplianceRecord
            {
                EntityId = employerId,
                Type = "Employer",
                Result = "Compliant",
                Notes = $"Non-compliance cleared: {notes}",
                OfficerId = officerId,
                Date = DateTime.Now
            });

            await _employers.SaveAsync();
            return (true, "Non-compliance flag cleared. Employer restored to 'Verified'.");
        }

        // ══════════════ Employer Review ══════════════

        public async Task<IEnumerable<Models.Employer>> GetAllEmployersAsync() =>
            await _employers.GetAllAsync();

        public async Task<IEnumerable<Models.Employer>> GetEmployersByStatusAsync(string status) =>
            await _employers.GetByStatusAsync(status);

        public async Task<Models.Employer?> GetEmployerDetailsAsync(int employerId) =>
            await _employers.GetWithDocumentsAsync(employerId);

        // ══════════════ Complaint Investigation ══════════════

        public async Task<IEnumerable<Complaint>> GetAllComplaintsAsync() =>
            await _complaints.GetAllAsync();

        public async Task<(bool, string)> ResolveComplaintAsync(
            int complaintId, int officerId, string resolution)
        {
            var complaint = await _complaints.GetByIdAsync(complaintId);
            if (complaint == null) return (false, "Complaint not found.");

            complaint.Status = "Resolved";
            complaint.ComplaintDescription +=
                $"\n\n--- Compliance Resolution ({DateTime.Now:yyyy-MM-dd}) ---\n{resolution}";
            _complaints.Update(complaint);
            await _complaints.SaveAsync();

            return (true, "Complaint resolved.");
        }

        // ══════════════ Compliance Records ══════════════

        public async Task<IEnumerable<ComplianceRecord>> GetAllComplianceRecordsAsync() =>
            await _compliance.GetAllAsync();

        public async Task<(bool, string)> CreateComplianceRecordAsync(ComplianceRecord record)
        {
            record.Date = DateTime.Now;
            await _compliance.AddAsync(record);
            await _compliance.SaveAsync();
            return (true, "Compliance record created.");
        }

        public async Task<(bool, string)> UpdateComplianceResultAsync(
            int recordId, string result, string notes)
        {
            var record = await _compliance.GetByIdAsync(recordId);
            if (record == null) return (false, "Record not found.");

            record.Result = result;
            record.Notes = (record.Notes ?? "") + $"\n[Updated {DateTime.Now:yyyy-MM-dd}] {notes}";
            record.Date = DateTime.Now;
            _compliance.Update(record);
            await _compliance.SaveAsync();
            return (true, $"Compliance result updated to '{result}'.");
        }

        // ══════════════ Violations ══════════════

        public async Task<IEnumerable<Violation>> GetAllViolationsAsync() =>
            await _violations.GetAllAsync();

        public async Task<(bool, string)> RecordViolationAsync(Violation violation)
        {
            violation.ViolationDate = DateTime.Now;
            await _violations.AddAsync(violation);
            await _violations.SaveAsync();
            return (true, "Violation recorded with penalty.");
        }

        // ══════════════ Dashboard ══════════════

        public async Task<ComplianceDashboardViewModel> GetDashboardAsync(int userId)
        {
            var pendingDocs = (await _employerDocs.GetPendingVerificationsAsync()).ToList();
            var employers = (await _employers.GetAllAsync()).ToList();
            var complaints = (await _complaints.GetAllAsync()).ToList();
            var violations = (await _violations.GetAllAsync()).ToList();
            var compliance = (await _compliance.GetAllAsync()).ToList();

            return new ComplianceDashboardViewModel
            {
                PendingReviews = pendingDocs.Count,
                TotalComplaints = complaints.Count,
                PendingComplaints = complaints.Count(c => c.Status == "Pending"),
                TotalViolations = violations.Count,
                TotalInspections = compliance.Count,
                Employers = employers,
                RecentComplaints = complaints.Take(10).ToList(),
                RecentViolations = violations.Take(10).ToList(),
                RecentRecords = compliance.Take(10).ToList()
            };
        }
    }
}
