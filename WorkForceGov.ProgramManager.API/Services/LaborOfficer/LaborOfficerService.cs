using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.LaborOfficer
{
    public class LaborOfficerService : ILaborOfficerService
    {
        private readonly ICitizenDocumentRepository _citizenDocs;
        private readonly IComplaintRepository _complaints;
        private readonly IComplianceRecordRepository _compliance;
        private readonly IViolationRepository _violations;
        private readonly IApplicationRepository _apps;
        private readonly INotificationRepository _notifications;

        public LaborOfficerService(
            ICitizenDocumentRepository citizenDocs,
            IComplaintRepository complaints,
            IComplianceRecordRepository compliance,
            IViolationRepository violations,
            IApplicationRepository apps,
            INotificationRepository notifications)
        {
            _citizenDocs = citizenDocs;
            _complaints = complaints;
            _compliance = compliance;
            _violations = violations;
            _apps = apps;
            _notifications = notifications;
        }

        // ══════════════ Document Verification ══════════════

        public async Task<IEnumerable<CitizenDocument>> GetPendingCitizenDocumentsAsync() =>
            await _citizenDocs.GetPendingVerificationsAsync();

        public async Task<(bool, string)> VerifyCitizenDocumentAsync(int documentId, int officerId)
        {
            var doc = await _citizenDocs.GetByIdAsync(documentId);
            if (doc == null) return (false, "Document not found.");

            doc.VerificationStatus = "Verified";
            doc.VerificationDate = DateTime.Now;
            doc.VerifiedByUserId = officerId;
            _citizenDocs.Update(doc);
            await _citizenDocs.SaveAsync();

            // Notify the citizen
            await _notifications.AddAsync(new Notification
            {
                UserId = doc.CitizenId, // Citizen's user ID will be resolved via FK
                Message = $"Your document '{doc.DocumentType}' has been verified.",
                Category = "DocumentVerification",
                EntityId = doc.Id,
                EntityType = "CitizenDocument"
            });
            await _notifications.SaveAsync();

            return (true, "Document verified successfully.");
        }

        public async Task<(bool, string)> RejectCitizenDocumentAsync(
            int documentId, int officerId, string reason)
        {
            var doc = await _citizenDocs.GetByIdAsync(documentId);
            if (doc == null) return (false, "Document not found.");

            doc.VerificationStatus = "Rejected";
            doc.RejectionReason = reason;
            doc.VerificationDate = DateTime.Now;
            doc.VerifiedByUserId = officerId;
            _citizenDocs.Update(doc);
            await _citizenDocs.SaveAsync();

            return (true, "Document rejected.");
        }

        // ══════════════ Complaint Management (cross-module) ══════════════
        // These complaints are created by CitizenService.RaiseComplaintAsync()
        // and consumed here by the Labor Officer for investigation.

        public async Task<IEnumerable<Complaint>> GetAllComplaintsAsync() =>
            await _complaints.GetAllAsync();

        public async Task<IEnumerable<Complaint>> GetPendingComplaintsAsync() =>
            await _complaints.GetPendingComplaintsAsync();

        /// <summary>
        /// Labor Officer investigates and resolves a complaint that was
        /// raised by a citizen through the Citizen module.
        /// </summary>
        public async Task<(bool, string)> InvestigateComplaintAsync(
            int complaintId, string resolution, string newStatus)
        {
            var complaint = await _complaints.GetByIdAsync(complaintId);
            if (complaint == null) return (false, "Complaint not found.");

            var validStatuses = new[] { "Under Investigation", "Resolved", "Dismissed" };
            if (!validStatuses.Contains(newStatus))
                return (false, $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

            complaint.Status = newStatus;
            complaint.ComplaintDescription += $"\n\n--- Officer Resolution ({DateTime.Now:yyyy-MM-dd}) ---\n{resolution}";
            _complaints.Update(complaint);
            await _complaints.SaveAsync();

            // Notify the citizen who raised the complaint
            await _notifications.AddAsync(new Notification
            {
                UserId = complaint.UserId,
                Message = $"Your complaint has been updated to '{newStatus}'.",
                Category = "ComplaintUpdate",
                EntityId = complaint.Id,
                EntityType = "Complaint"
            });
            await _notifications.SaveAsync();

            return (true, $"Complaint updated to '{newStatus}'.");
        }

        // ══════════════ Compliance ══════════════

        public async Task<IEnumerable<ComplianceRecord>> GetComplianceRecordsAsync() =>
            await _compliance.GetAllAsync();

        public async Task<(bool, string)> CreateComplianceRecordAsync(ComplianceRecord record)
        {
            record.Date = DateTime.Now;
            await _compliance.AddAsync(record);
            await _compliance.SaveAsync();
            return (true, "Compliance record created.");
        }

        // ══════════════ Violations ══════════════

        public async Task<IEnumerable<Violation>> GetViolationsAsync() =>
            await _violations.GetAllAsync();

        public async Task<(bool, string)> RecordViolationAsync(Violation violation)
        {
            violation.ViolationDate = DateTime.Now;
            await _violations.AddAsync(violation);
            await _violations.SaveAsync();
            return (true, "Violation recorded.");
        }

        // ══════════════ Application Oversight ══════════════

        public async Task<IEnumerable<Application>> GetAllApplicationsAsync()
        {
            return await _apps.GetAllAsync();
        }

        public async Task<(bool, string)> FlagApplicationAsync(int applicationId, string notes)
        {
            var app = await _apps.GetByIdAsync(applicationId);
            if (app == null) return (false, "Application not found.");
            app.ReviewNotes = $"[FLAGGED by Labor Officer] {notes}";
            app.Status = "Flagged";
            _apps.Update(app);
            await _apps.SaveAsync();
            return (true, "Application flagged for review.");
        }

        // ══════════════ Dashboard ══════════════

        public async Task<LaborOfficerDashboardViewModel> GetDashboardAsync(int userId)
        {
            var pendingDocs = (await _citizenDocs.GetPendingVerificationsAsync()).ToList();
            var pendingComplaints = (await _complaints.GetPendingComplaintsAsync()).ToList();
            var allApps = (await _apps.GetAllAsync()).ToList();
            var notifications = (await _notifications.GetByUserAsync(userId, 10)).ToList();

            return new LaborOfficerDashboardViewModel
            {
                PendingDocuments = pendingDocs.Count,
                ComplianceAlerts = pendingComplaints.Count,
                PendingApplications = allApps.Count(a => a.Status == "Pending"),
                ApprovedApplications = allApps.Count(a => a.Status == "Approved"),
                RejectedApplications = allApps.Count(a => a.Status == "Rejected"),
                PendingDocs = pendingDocs,
                RecentApplications = allApps.Take(10).ToList(),
                FlaggedEmployers = new List<Models.Employer>(),
                Notifications = notifications
            };
        }
    }
}
