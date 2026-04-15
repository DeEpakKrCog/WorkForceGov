using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IJobService
    {
        Task<IEnumerable<JobOpening>> GetAllJobsAsync();
        Task<JobOpening?> GetJobByIdAsync(int id);
        Task<IEnumerable<JobOpening>> GetJobsByEmployerAsync(int employerId);
        Task<IEnumerable<JobOpening>> GetOpenJobsAsync();
        Task<IEnumerable<JobOpening>> GetByEmployerAsync(int employerId);
        Task<JobOpening?> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CreateAsync(object model);
        Task<(bool Success, string Message)> UpdateAsync(object model);
        Task<(bool Success, string Message)> CloseAsync(int jobId);
        Task<IEnumerable<JobOpening>> SearchAsync(string searchTerm);
        Task<IEnumerable<JobOpening>> SearchAsync(string keyword, string location, string category);
    }
}
