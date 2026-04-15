using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Employer
{
    public class JobService : IJobService
    {
        private readonly IJobOpeningRepository _jobRepository;
        private readonly IEmployerRepository _employerRepository;

        public JobService(IJobOpeningRepository jobRepository, IEmployerRepository employerRepository)
        {
            _jobRepository = jobRepository;
            _employerRepository = employerRepository;
        }

        public async Task<IEnumerable<JobOpening>> GetAllJobsAsync()
        {
            return await _jobRepository.GetAllAsync();
        }

        public async Task<JobOpening?> GetJobByIdAsync(int id)
        {
            return await _jobRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<JobOpening>> GetJobsByEmployerAsync(int employerId)
        {
            var jobs = await _jobRepository.GetAllAsync();
            return jobs.Where(j => j.EmployerId == employerId).ToList();
        }

        public async Task<IEnumerable<JobOpening>> GetOpenJobsAsync()
        {
            var jobs = await _jobRepository.GetAllAsync();
            return jobs.Where(j => j.Status == "Open").ToList();
        }

        public async Task<IEnumerable<JobOpening>> GetByEmployerAsync(int employerId)
        {
            var jobs = await _jobRepository.GetAllAsync();
            return jobs.Where(j => j.EmployerId == employerId).ToList();
        }

        public async Task<JobOpening?> GetByIdAsync(int id)
        {
            return await _jobRepository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> CreateAsync(object model)
        {
            try
            {
                if (model is JobOpening job)
                {
                    var employer = await _employerRepository.GetByIdAsync(job.EmployerId);
                    if (employer == null) return (false, "Employer not found.");

                    // STRICT CHECK: Only "Verified" status allowed
                    if (employer.Status != "Verified")
                    {
                        return (false, "Verification Required: You must upload your Business License/PAN and wait for Labor Officer approval before posting jobs.");
                    }

                    job.PostedDate = DateTime.Now;
                    job.Status = "Open";
                    await _jobRepository.AddAsync(job);
                    await _jobRepository.SaveAsync();
                    return (true, "Job opening created successfully");
                }
                return (false, "Invalid job data");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(object model)
        {
            try
            {
                if (model is JobOpening job)
                {
                    _jobRepository.Update(job);
                    await _jobRepository.SaveAsync();
                    return (true, "Job opening updated successfully");
                }
                return (false, "Invalid job data");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating job: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CloseAsync(int jobId)
        {
            try
            {
                var job = await _jobRepository.GetByIdAsync(jobId);
                if (job == null)
                    return (false, "Job opening not found");

                job.Status = "Closed";
                _jobRepository.Update(job);
                await _jobRepository.SaveAsync();
                return (true, "Job opening closed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error closing job: {ex.Message}");
            }
        }

        public async Task<IEnumerable<JobOpening>> SearchAsync(string searchTerm)
        {
            var jobs = await _jobRepository.GetAllAsync();
            return jobs.Where(j => j.JobTitle.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   j.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<IEnumerable<JobOpening>> SearchAsync(string keyword, string location, string category)
        {
            var jobs = await _jobRepository.GetAllAsync();
            return jobs.Where(j => 
                (string.IsNullOrEmpty(keyword) || j.JobTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                 j.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(location) || j.Location.Contains(location, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(category) || j.JobCategory.Contains(category, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }
}
