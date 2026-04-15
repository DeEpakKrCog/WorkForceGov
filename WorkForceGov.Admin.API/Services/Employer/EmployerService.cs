using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.Employer
{
    public class EmployerService : IEmployerService
    {
        private readonly IEmployerRepository _employers;
        private readonly IEmployerDocumentRepository _docs;
        private readonly IJobOpeningRepository _jobs;
        private readonly IApplicationRepository _apps;
        private readonly INotificationRepository _notifications;

        public EmployerService(
            IEmployerRepository employers,
            IEmployerDocumentRepository docs,
            IJobOpeningRepository jobs,
            IApplicationRepository apps,
            INotificationRepository notifications)
        {
            _employers = employers;
            _docs = docs;
            _jobs = jobs;
            _apps = apps;
            _notifications = notifications;
        }

        // ══════════════ Profile ══════════════

        public async Task<Models.Employer?> GetByIdAsync(int id) => await _employers.GetByIdAsync(id);

        public async Task<Models.Employer?> GetByUserIdAsync(int userId) =>
            await _employers.GetByUserIdAsync(userId);

        public async Task<(bool, string)> RegisterAsync(int userId, Models.Employer employer)
        {
            if (await _employers.AnyAsync(e => e.UserId == userId))
                return (false, "Employer profile already exists.");

            employer.UserId = userId;
            employer.Status = "Pending"; // Always starts as Pending
            await _employers.AddAsync(employer);
            await _employers.SaveAsync();
            return (true, "Employer registered. Upload documents for compliance verification.");
        }

        public async Task<(bool, string)> RegisterEmployerAsync(int userId, object model)
        {
            try
            {
                if (await _employers.AnyAsync(e => e.UserId == userId))
                    return (false, "Employer profile already exists.");

                var employer = model as Models.Employer ?? new Models.Employer();
                employer.UserId = userId;
                employer.Status = "Pending";
                await _employers.AddAsync(employer);
                await _employers.SaveAsync();
                return (true, "Employer registered successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error registering employer: {ex.Message}");
            }
        }

        public async Task<(bool, string)> UpdateProfileAsync(Models.Employer employer)
        {
            _employers.Update(employer);
            await _employers.SaveAsync();
            return (true, "Profile updated.");
        }

        // ══════════════ Document Upload (cross-module trigger) ══════════════

        public async Task<IEnumerable<EmployerDocument>> GetDocumentsAsync(int employerId) =>
            await _docs.GetByEmployerAsync(employerId);

        /// <summary>
        /// Uploads a document and AUTOMATICALLY sets the employer status to "Pending"
        /// until the Compliance Officer reviews and approves through their Verification Section.
        /// This is the cross-module trigger that connects EmployerService → ComplianceOfficerService.
        /// </summary>
        public async Task<(bool, string, EmployerDocument?)> UploadDocumentAsync(
            int employerId, string docType, string fileUrl)
        {
            var employer = await _employers.GetByIdAsync(employerId);
            if (employer == null) return (false, "Employer not found.", null);

            // Create the document record
            var doc = new EmployerDocument
            {
                EmployerId = employerId,
                DocType = docType,
                FileURL = fileUrl,
                VerificationStatus = "Pending",
                UploadedDate = DateTime.Now
            };
            await _docs.AddAsync(doc);

            // ── CROSS-MODULE LOGIC ──
            // Flag employer status as "Pending" whenever new documents are uploaded.
            // The Compliance Officer must approve ALL documents before employer
            // status can transition back to "Verified".
            if (employer.Status != "Pending")
            {
                employer.Status = "Pending";
                _employers.Update(employer);
            }

            await _docs.SaveAsync();

            return (true,
                "Document uploaded. Employer status set to 'Pending' until Compliance Officer verification.",
                doc);
        }

        // ══════════════ Job Management ══════════════

        public async Task<IEnumerable<JobOpening>> GetJobsAsync(int employerId) =>
            await _jobs.GetByEmployerAsync(employerId);

        public async Task<(bool, string)> PostJobAsync(JobOpening job)
        {
            // Only verified employers can post jobs
            var employer = await _employers.GetByIdAsync(job.EmployerId);
            if (employer == null) return (false, "Employer not found.");
            if (employer.Status != "Verified")
                return (false, $"Cannot post jobs. Employer status is '{employer.Status}'. Complete compliance verification first.");

            await _jobs.AddAsync(job);
            await _jobs.SaveAsync();
            return (true, "Job posted successfully.");
        }

        public async Task<(bool, string)> UpdateJobAsync(JobOpening job)
        {
            _jobs.Update(job);
            await _jobs.SaveAsync();
            return (true, "Job updated.");
        }

        public async Task<(bool, string)> CloseJobAsync(int jobId)
        {
            var job = await _jobs.GetByIdAsync(jobId);
            if (job == null) return (false, "Job not found.");
            job.Status = "Closed";
            _jobs.Update(job);
            await _jobs.SaveAsync();
            return (true, "Job closed.");
        }

        // ══════════════ Application Management ══════════════

        public async Task<IEnumerable<Application>> GetApplicationsReceivedAsync(int employerId) =>
            await _apps.GetByEmployerAsync(employerId);

        public async Task<Application?> GetApplicationDetailsAsync(int applicationId) =>
            await _apps.GetWithDetailsAsync(applicationId);

        public async Task<(bool, string)> UpdateApplicationStatusAsync(
            int applicationId, string status, string? notes)
        {
            var app = await _apps.GetByIdAsync(applicationId);
            if (app == null) return (false, "Application not found.");

            app.Status = status;
            app.ReviewNotes = notes;
            app.ReviewedDate = DateTime.Now;
            _apps.Update(app);
            await _apps.SaveAsync();
            return (true, $"Application status updated to '{status}'.");
        }

        // ══════════════ Dashboard ══════════════

        public async Task<EmployerDashboardViewModel> GetDashboardAsync(int userId)
        {
            var employer = await _employers.GetByUserIdAsync(userId);
            if (employer == null)
                return new EmployerDashboardViewModel { Employer = new Models.Employer() };

            var jobs = (await _jobs.GetByEmployerAsync(employer.Id)).ToList();
            var apps = (await _apps.GetByEmployerAsync(employer.Id)).ToList();
            var notifications = (await _notifications.GetByUserAsync(userId, 10)).ToList();

            return new EmployerDashboardViewModel
            {
                Employer = employer,
                TotalJobPostings = jobs.Count,
                OpenJobPostings = jobs.Count(j => j.Status == "Open"),
                TotalApplicationsReceived = apps.Count,
                PendingApplications = apps.Count(a => a.Status == "Pending"),
                ShortlistedCandidates = apps.Count(a => a.Status == "Shortlisted"),
                HiredCandidates = apps.Count(a => a.Status == "Approved"),
                RecentJobs = jobs.Take(5).ToList(),
                RecentApplications = apps.Take(10).ToList(),
                Notifications = notifications
            };
        }
    }
}
