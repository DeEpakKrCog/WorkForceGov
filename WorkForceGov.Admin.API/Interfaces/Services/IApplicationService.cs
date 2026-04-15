using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IApplicationService
    {
        Task<IEnumerable<Application>> GetApplicationsAsync();
        Task<Application?> GetApplicationByIdAsync(int id);
        Task<IEnumerable<Application>> GetApplicationsByCitizenAsync(int citizenId);
        Task<IEnumerable<Application>> GetApplicationsByJobAsync(int jobOpeningId);
        Task<IEnumerable<Application>> GetByJobAsync(int jobOpeningId);
        Task<IEnumerable<Application>> GetByEmployerAsync(int employerId);
        Task<Application?> GetWithDetailsAsync(int id);
        Task<IEnumerable<Application>> GetAllWithDetailsAsync();
        Task<(bool Success, string Message)> UpdateStatusAsync(int applicationId, string status, string? notes = null);
        Task<(bool Success, string Message)> ApplyAsync(int citizenId, int jobOpeningId);
        Task<(bool Success, string Message)> ApplyAsync(int citizenId, int jobOpeningId, string? coverLetter);
        Task<IEnumerable<Application>> GetByCitizenAsync(int citizenId);
        Task<(bool Success, string Message)> WithdrawAsync(int applicationId);
    }
}
