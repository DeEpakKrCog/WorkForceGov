using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.Citizen
{
    public class CitizenService : ICitizenService
    {
        private readonly ICitizenRepository _citizens;
        private readonly ICitizenDocumentRepository _docs;
        private readonly IApplicationRepository _apps;
        private readonly IBenefitRepository _benefits;
        private readonly IComplaintRepository _complaints;
        private readonly IJobOpeningRepository _jobs;
        private readonly INotificationRepository _notifications;

        public CitizenService(
            ICitizenRepository citizens,
            ICitizenDocumentRepository docs,
            IApplicationRepository apps,
            IBenefitRepository benefits,
            IComplaintRepository complaints,
            IJobOpeningRepository jobs,
            INotificationRepository notifications)
        {
            _citizens = citizens;
            _docs = docs;
            _apps = apps;
            _benefits = benefits;
            _complaints = complaints;
            _jobs = jobs;
            _notifications = notifications;
        }

        // ══════════════ Profile ══════════════

        public async Task<Models.Citizen?> GetByIdAsync(int id) =>
            await _citizens.GetByIdAsync(id);

        public async Task<Models.Citizen?> GetByUserIdAsync(int userId) =>
            await _citizens.GetByUserIdAsync(userId);

        public async Task<(bool, string)> CreateProfileAsync(int userId, string name, string email)
        {
            if (await _citizens.AnyAsync(c => c.UserId == userId))
                return (false, "Profile already exists.");

            await _citizens.AddAsync(new Models.Citizen
            {
                UserId = userId,
                FullName = name,
                Email = email
            });
            await _citizens.SaveAsync();
            return (true, "Citizen profile created successfully.");
        }

        public async Task<(bool, string)> UpdateProfileAsync(Models.Citizen citizen)
        {
            _citizens.Update(citizen);
            await _citizens.SaveAsync();
            return (true, "Profile updated successfully.");
        }

        // ══════════════ Documents ══════════════

        public async Task<IEnumerable<CitizenDocument>> GetDocumentsAsync(int citizenId) =>
            await _docs.GetByCitizenAsync(citizenId);

        public async Task<(bool, string)> UploadDocumentAsync(
            int citizenId, string docType, string fileName, string filePath)
        {
            await _docs.AddAsync(new CitizenDocument
            {
                CitizenId = citizenId,
                DocumentType = docType,
                FileName = fileName,
                FilePath = filePath
            });
            await _docs.SaveAsync();
            return (true, "Document uploaded. Pending verification by Labor Officer.");
        }

        // ══════════════ Job Applications ══════════════

        public async Task<IEnumerable<Application>> GetApplicationsAsync(int citizenId) =>
            await _apps.GetByCitizenAsync(citizenId);

        public async Task<(bool, string)> ApplyForJobAsync(int citizenId, int jobId, string? coverLetter)
        {
            if (await _apps.HasAppliedAsync(citizenId, jobId))
                return (false, "You have already applied to this job.");

            var job = await _jobs.GetByIdAsync(jobId);
            if (job == null || job.Status != "Open")
                return (false, "This job is no longer accepting applications.");

            // ─── RELAXED BUSINESS RULE: Only check if document exists ───
            var docs = (await _docs.GetByCitizenAsync(citizenId)).ToList();

            // Check if any document type contains "resume" (ignore verification status)
            var hasResume = docs.Any(d => d.DocumentType.ToLower().Contains("resume"));

            if (!hasResume)
                return (false, "You must upload your resume before applying for jobs.");

            // REMOVED: Identity verification check (as per your request to apply immediately)

            await _apps.AddAsync(new Application
            {
                CitizenId = citizenId,
                JobOpeningId = jobId,
                CoverLetter = coverLetter,
                Status = "Pending",
                SubmittedDate = DateTime.Now
            });
            await _apps.SaveAsync();
            return (true, "Application submitted successfully.");
        }

        public async Task<(bool, string)> WithdrawApplicationAsync(int applicationId)
        {
            var app = await _apps.GetByIdAsync(applicationId);
            if (app == null) return (false, "Application not found.");
            if (app.Status != "Pending")
                return (false, "Only pending applications can be withdrawn.");

            app.Status = "Withdrawn";
            _apps.Update(app);
            await _apps.SaveAsync();
            return (true, "Application withdrawn.");
        }

        // ══════════════ Benefits ══════════════

        public async Task<IEnumerable<Benefit>> GetBenefitsAsync(int citizenId) =>
            await _benefits.FindAsync(b => b.CitizenId == citizenId);

        // ══════════════ Complaints (cross-module) ══════════════

        public async Task<IEnumerable<Complaint>> GetMyComplaintsAsync(int userId) =>
            await _complaints.GetByCitizenUserIdAsync(userId);

        /// <summary>
        /// Citizen raises a complaint against an employer.
        /// This complaint becomes visible and actionable in the LaborOfficerService
        /// and ComplianceOfficerService through their own interfaces.
        /// </summary>
        public async Task<(bool, string, Complaint?)> RaiseComplaintAsync(
            int userId, int employerId, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return (false, "Complaint description is required.", null);

            var complaint = new Complaint
            {
                UserId = userId,
                EmployerId = employerId,
                ComplaintDescription = description,
                Status = "Pending",
                SubmittedDate = DateTime.Now
            };

            await _complaints.AddAsync(complaint);
            await _complaints.SaveAsync();

            return (true, "Complaint filed successfully. A Labor Officer will investigate.", complaint);
        }

        // ══════════════ Dashboard ══════════════

        public async Task<CitizenDashboardViewModel> GetDashboardAsync(int userId)
        {
            var citizen = await _citizens.GetByUserIdAsync(userId);
            if (citizen == null)
                return new CitizenDashboardViewModel { Citizen = new Models.Citizen() };

            var apps = (await _apps.GetByCitizenAsync(citizen.Id)).ToList();
            var benefits = (await _benefits.FindAsync(b => b.CitizenId == citizen.Id)).ToList();
            var docs = (await _docs.GetByCitizenAsync(citizen.Id)).ToList();
            var notifications = (await _notifications.GetByUserAsync(userId, 10)).ToList();
            var recommendedJobs = (await _jobs.GetOpenJobsAsync()).Take(5).ToList();

            return new CitizenDashboardViewModel
            {
                Citizen = citizen,
                ActiveApplications = apps.Count(a => a.Status == "Pending" || a.Status == "Under Review"),
                TotalBenefits = benefits.Count,
                TotalBenefitAmount = benefits.Sum(b => b.Amount),
                DocumentCount = docs.Count,
                PendingDocs = docs.Count(d => d.VerificationStatus == "Pending"),
                VerifiedDocs = docs.Count(d => d.VerificationStatus == "Verified"),
                RecentApplications = apps.Take(5).ToList(),
                Notifications = notifications,
                RecommendedJobs = recommendedJobs.ToList()
            };
        }
    }
}
