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

        public async Task<(bool Success, string Message)> RegisterAsync(int userId, Models.Employer employer)
        {
            if (await _employers.AnyAsync(e => e.UserId == userId))
                return (false, "Employer profile already exists.");

            employer.UserId = userId;
            employer.Status = "Pending";
            await _employers.AddAsync(employer);

            await _notifications.AddAsync(new Notification
            {
                UserId = userId,
                Message = "Welcome! Your registration is successful. Please upload documents for verification.",
                CreatedDate = DateTime.Now,
                IsRead = false
            });

            await _employers.SaveAsync();
            await _notifications.SaveAsync();
            return (true, "Registered successfully.");
        }

        public async Task<(bool Success, string Message)> RegisterEmployerAsync(int userId, object model)
        {
            if (model is Models.Employer emp) return await RegisterAsync(userId, emp);
            return (false, "Invalid employer data.");
        }

        public async Task<(bool Success, string Message)> UpdateProfileAsync(Models.Employer employer)
        {
            _employers.Update(employer);
            await _employers.SaveAsync();
            return (true, "Profile updated successfully.");
        }

        // ══════════════ Document Upload ══════════════

        public async Task<IEnumerable<EmployerDocument>> GetDocumentsAsync(int employerId) =>
            await _docs.GetByEmployerAsync(employerId);

        public async Task<(bool Success, string Message, EmployerDocument? Document)> UploadDocumentAsync(
            int employerId, string docType, string fileUrl)
        {
            var employer = await _employers.GetByIdAsync(employerId);
            if (employer == null) return (false, "Employer not found.", null);

            var doc = new EmployerDocument
            {
                EmployerId = employerId,
                DocType = docType,
                FileURL = fileUrl,
                VerificationStatus = "Pending",
                UploadedDate = DateTime.Now
            };
            await _docs.AddAsync(doc);

            // 🚨 LOGIC FIX: Check if they ALREADY have a verified document
            var existingDocs = await _docs.GetByEmployerAsync(employerId);
            bool hasVerifiedDoc = existingDocs.Any(d => d.VerificationStatus == "Verified");

            // If they have NO verified documents, they stay/become Pending.
            // If they DO have a verified document, they stay/become Verified.
            if (hasVerifiedDoc)
            {
                if (employer.Status != "Verified")
                {
                    employer.Status = "Verified";
                    _employers.Update(employer);
                }
            }
            else
            {
                if (employer.Status != "Pending")
                {
                    employer.Status = "Pending";
                    _employers.Update(employer);
                }
            }

            await _notifications.AddAsync(new Notification
            {
                UserId = employer.UserId,
                Message = $"Your {docType} has been uploaded and is pending review.",
                CreatedDate = DateTime.Now,
                IsRead = false
            });

            await _docs.SaveAsync();
            await _notifications.SaveAsync();

            return (true, "Document uploaded successfully.", doc);
        }

        // ══════════════ Job Management ══════════════

        public async Task<IEnumerable<JobOpening>> GetJobsAsync(int employerId) =>
            await _jobs.GetByEmployerAsync(employerId);

        public async Task<(bool Success, string Message)> PostJobAsync(JobOpening job)
        {
            var employer = await _employers.GetByIdAsync(job.EmployerId);
            if (employer?.Status != "Verified")
                return (false, "Only verified employers can post jobs.");

            await _jobs.AddAsync(job);
            await _jobs.SaveAsync();
            return (true, "Job posted successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateJobAsync(JobOpening job)
        {
            _jobs.Update(job);
            await _jobs.SaveAsync();
            return (true, "Job updated successfully.");
        }

        public async Task<(bool Success, string Message)> CloseJobAsync(int jobId)
        {
            var job = await _jobs.GetByIdAsync(jobId);
            if (job == null) return (false, "Job not found.");

            job.Status = "Closed";
            _jobs.Update(job);
            await _jobs.SaveAsync();
            return (true, "Job closed successfully.");
        }

        // ══════════════ Application Management ══════════════

        public async Task<IEnumerable<Application>> GetApplicationsReceivedAsync(int employerId) =>
            await _apps.GetByEmployerAsync(employerId);

        public async Task<Application?> GetApplicationDetailsAsync(int applicationId) =>
            await _apps.GetWithDetailsAsync(applicationId);

        public async Task<(bool Success, string Message)> UpdateApplicationStatusAsync(
            int applicationId, string status, string? notes)
        {
            var app = await _apps.GetWithDetailsAsync(applicationId);
            if (app == null) return (false, "Application not found.");

            app.Status = status;
            app.ReviewNotes = notes;
            app.ReviewedDate = DateTime.Now;
            _apps.Update(app);

            if (app.Citizen != null)
            {
                await _notifications.AddAsync(new Notification
                {
                    UserId = app.Citizen.UserId,
                    Message = $"Status Update: Your application for '{app.JobOpening?.JobTitle}' is now {status}.",
                    CreatedDate = DateTime.Now,
                    IsRead = false
                });
            }

            await _apps.SaveAsync();
            await _notifications.SaveAsync();
            return (true, "Status updated successfully.");
        }

        // ══════════════ Dashboard ══════════════

        // ══════════════ Dashboard ══════════════

        public async Task<EmployerDashboardViewModel> GetDashboardAsync(int userId)
        {
            var employer = await _employers.GetByUserIdAsync(userId);
            if (employer == null) return new EmployerDashboardViewModel { Employer = new Models.Employer() };

            // 🚨 SELF-HEALING FIX: Automatically correct profile status based on documents
            var employerDocs = await _docs.GetByEmployerAsync(employer.Id);
            bool hasVerifiedDoc = employerDocs.Any(d => d.VerificationStatus == "Verified");

            // If they have a verified document but their profile is stuck on Pending, fix it!
            if (hasVerifiedDoc && employer.Status != "Verified")
            {
                employer.Status = "Verified";
                _employers.Update(employer);
                await _employers.SaveAsync(); // Save the fixed status to the database
            }

            var jobs = (await _jobs.GetByEmployerAsync(employer.Id)).ToList();
            var apps = (await _apps.GetByEmployerAsync(employer.Id)).ToList();
            var notifications = (await _notifications.GetByUserAsync(userId, 10)).ToList();

            return new EmployerDashboardViewModel
            {
                Employer = employer,
                TotalJobPostings = jobs.Count,
                TotalApplicationsReceived = apps.Count,
                ShortlistedCandidates = apps.Count(a => a.Status == "Shortlisted"),
                HiredCandidates = apps.Count(a => a.Status == "Approved"),
                RecentJobs = jobs.OrderByDescending(j => j.PostedDate).Take(5).ToList(),
                RecentApplications = apps.OrderByDescending(a => a.SubmittedDate).Take(10).ToList(),
                Notifications = notifications
            };
        }
    }
}