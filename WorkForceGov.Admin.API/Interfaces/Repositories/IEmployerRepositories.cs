using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Repositories
{
    /// <summary>
    /// Data access for Employer profiles.
    /// </summary>
    public interface IEmployerRepository : IRepository<Employer>
    {
        Task<Employer?> GetByUserIdAsync(int userId);
        Task<Employer?> GetWithJobsAsync(int employerId);
        Task<Employer?> GetWithDocumentsAsync(int employerId);
        Task<IEnumerable<Employer>> GetByStatusAsync(string status);
    }

    /// <summary>
    /// Data access for employer-uploaded documents (license, registration, etc.).
    /// Cross-module: documents uploaded here are reviewed by the Compliance Officer.
    /// Upload triggers employer status → "Pending".
    /// </summary>
    public interface IEmployerDocumentRepository : IRepository<EmployerDocument>
    {
        Task<IEnumerable<EmployerDocument>> GetByEmployerAsync(int employerId);
        Task<IEnumerable<EmployerDocument>> GetPendingVerificationsAsync();
    }

    /// <summary>
    /// Data access for job openings posted by employers.
    /// </summary>
    public interface IJobOpeningRepository : IRepository<JobOpening>
    {
        Task<IEnumerable<JobOpening>> GetOpenJobsAsync();
        Task<IEnumerable<JobOpening>> SearchAsync(string? keyword, string? location, string? category);
        Task<JobOpening?> GetWithApplicationsAsync(int jobId);
        Task<IEnumerable<JobOpening>> GetByEmployerAsync(int employerId);
    }
}
