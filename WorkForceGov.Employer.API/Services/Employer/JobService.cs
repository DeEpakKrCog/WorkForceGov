using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Employer
{
    public class JobService : IJobService
    {
        private readonly IJobOpeningRepository _jobRepository;
        private readonly IEmployerRepository _employerRepository;
        private readonly INotificationRepository _notifications;

        public JobService(
            IJobOpeningRepository jobRepository,
            IEmployerRepository employerRepository,
            INotificationRepository notifications)
        {
            _jobRepository = jobRepository;
            _employerRepository = employerRepository;
            _notifications = notifications;
        }

        public async Task<IEnumerable<JobOpening>> GetAllJobsAsync() => await _jobRepository.GetAllAsync();

        public async Task<JobOpening?> GetByIdAsync(int id) => await _jobRepository.GetByIdAsync(id);

        public async Task<JobOpening?> GetJobByIdAsync(int id) => await GetByIdAsync(id);

        public async Task<IEnumerable<JobOpening>> GetJobsByEmployerAsync(int employerId) => await _jobRepository.GetByEmployerAsync(employerId);

        public async Task<IEnumerable<JobOpening>> GetByEmployerAsync(int employerId) => await _jobRepository.GetByEmployerAsync(employerId);

        // 🚨 FIX: Strictly return ONLY Open status jobs
        public async Task<IEnumerable<JobOpening>> GetOpenJobsAsync() =>
            (await _jobRepository.GetAllAsync()).Where(j => j.Status == "Open");

        public async Task<(bool Success, string Message)> CreateAsync(object model)
        {
            try
            {
                if (model is not JobOpening job) return (false, "Invalid job data.");
                var employer = await _employerRepository.GetByIdAsync(job.EmployerId);
                if (employer == null) return (false, "Employer not found.");
                if (employer.Status != "Verified") return (false, "Verification Required.");

                job.PostedDate = DateTime.Now;
                job.Status = "Open";
                await _jobRepository.AddAsync(job);

                await _notifications.AddAsync(new Notification
                {
                    UserId = employer.UserId,
                    Message = $"Success: Your job '{job.JobTitle}' is now live.",
                    CreatedDate = DateTime.Now
                });

                await _jobRepository.SaveAsync();
                await _notifications.SaveAsync();
                return (true, "Job opening created successfully");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(object model)
        {
            try
            {
                if (model is not JobOpening job) return (false, "Invalid data.");
                var existing = await _jobRepository.GetByIdAsync(job.Id);
                if (existing == null) return (false, "Job not found.");

                existing.JobTitle = job.JobTitle;
                existing.Description = job.Description;
                existing.Location = job.Location;
                existing.Status = job.Status; // Allows manual status management

                _jobRepository.Update(existing);
                await _jobRepository.SaveAsync();
                return (true, "Job updated successfully");
            }
            catch (Exception ex) { return (false, $"Error: {ex.Message}"); }
        }

        public async Task<(bool Success, string Message)> CloseAsync(int jobId)
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null) return (false, "Job not found.");
            job.Status = "Closed";
            _jobRepository.Update(job);
            await _jobRepository.SaveAsync();
            return (true, "Job closed successfully");
        }

        // 🚨 FIX: Filter search results to exclude "Closed" jobs
        public async Task<IEnumerable<JobOpening>> SearchAsync(string keyword, string location, string category)
        {
            var results = await _jobRepository.SearchAsync(keyword, location, category);
            return results.Where(j => j.Status == "Open");
        }

        public async Task<IEnumerable<JobOpening>> SearchAsync(string searchTerm) =>
            (await _jobRepository.SearchAsync(searchTerm, null, null)).Where(j => j.Status == "Open");
    }
}