using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    /// <summary>
    /// Business logic for the Employer module.
    /// Covers company registration, job posting, application management,
    /// and a Document Upload system that flags employer status as "Pending"
    /// until a Compliance Officer approves them.
    /// </summary>
    public interface IEmployerService
    {
        // ── Profile ──
        Task<Employer?> GetByIdAsync(int id);
        Task<Employer?> GetByUserIdAsync(int userId);
        Task<(bool Success, string Message)> RegisterAsync(int userId, Employer employer);
        Task<(bool Success, string Message)> RegisterEmployerAsync(int userId, object model);
        Task<(bool Success, string Message)> UpdateProfileAsync(Employer employer);

        // ── Document Upload (cross-module: triggers Compliance review) ──
        /// <summary>
        /// Uploads a document and automatically sets the employer's status to "Pending"
        /// until a Compliance Officer reviews and approves through the Verification Section.
        /// </summary>
        Task<IEnumerable<EmployerDocument>> GetDocumentsAsync(int employerId);
        Task<(bool Success, string Message, EmployerDocument? Document)> UploadDocumentAsync(
            int employerId, string docType, string fileUrl);

        // ── Job Management ──
        Task<IEnumerable<JobOpening>> GetJobsAsync(int employerId);
        Task<(bool Success, string Message)> PostJobAsync(JobOpening job);
        Task<(bool Success, string Message)> UpdateJobAsync(JobOpening job);
        Task<(bool Success, string Message)> CloseJobAsync(int jobId);

        // ── Application Management ──
        Task<IEnumerable<Application>> GetApplicationsReceivedAsync(int employerId);
        Task<Application?> GetApplicationDetailsAsync(int applicationId);
        Task<(bool Success, string Message)> UpdateApplicationStatusAsync(int applicationId, string status, string? notes);

        // ── Dashboard ──
        Task<EmployerDashboardViewModel> GetDashboardAsync(int userId);
    }
}
