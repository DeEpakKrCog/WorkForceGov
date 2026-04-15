using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    /// <summary>
    /// Business logic for the Citizen / Job Seeker module.
    /// Covers profile management, job applications, document uploads,
    /// benefit tracking, and complaint filing against employers.
    /// </summary>
    public interface ICitizenService
    {
        // ── Profile ──
        Task<Citizen?> GetByIdAsync(int id);
        Task<Citizen?> GetByUserIdAsync(int userId);
        Task<(bool Success, string Message)> CreateProfileAsync(int userId, string name, string email);
        Task<(bool Success, string Message)> UpdateProfileAsync(Citizen citizen);

        // ── Documents ──
        Task<IEnumerable<CitizenDocument>> GetDocumentsAsync(int citizenId);
        Task<(bool Success, string Message)> UploadDocumentAsync(int citizenId, string docType, string fileName, string filePath);

        // ── Job Applications ──
        Task<IEnumerable<Application>> GetApplicationsAsync(int citizenId);
        Task<(bool Success, string Message)> ApplyForJobAsync(int citizenId, int jobId, string? coverLetter);
        Task<(bool Success, string Message)> WithdrawApplicationAsync(int applicationId);

        // ── Benefits ──
        Task<IEnumerable<Benefit>> GetBenefitsAsync(int citizenId);

        // ── Complaints (cross-module: surfaces in LaborOfficer) ──
        Task<IEnumerable<Complaint>> GetMyComplaintsAsync(int userId);
        Task<(bool Success, string Message, Complaint? Complaint)> RaiseComplaintAsync(int userId, int employerId, string description);

        // ── Dashboard ──
        Task<CitizenDashboardViewModel> GetDashboardAsync(int userId);
    }
}
