using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    /// <summary>
    /// Business logic for managing Job Openings.
    /// </summary>
    public interface IJobService
    {
        // Retrieval
        Task<IEnumerable<JobOpening>> GetAllJobsAsync();
        Task<JobOpening?> GetByIdAsync(int id); // Combined identity method
        Task<IEnumerable<JobOpening>> GetByEmployerAsync(int employerId); // Combined employer method
        Task<IEnumerable<JobOpening>> GetOpenJobsAsync();

        // Actions
        Task<(bool Success, string Message)> CreateAsync(object model);
        Task<(bool Success, string Message)> UpdateAsync(object model);
        Task<(bool Success, string Message)> CloseAsync(int jobId);

        // Search
        Task<IEnumerable<JobOpening>> SearchAsync(string searchTerm);
        Task<IEnumerable<JobOpening>> SearchAsync(string keyword, string location, string category);
    }
}