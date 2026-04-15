using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IReportService
    {
        Task<IEnumerable<Report>> GetAllReportsAsync();
        Task<IEnumerable<Report>> GetAllAsync();
        Task<Report?> GetReportByIdAsync(int id);
        Task<IEnumerable<Report>> GetReportsByUserAsync(int userId);
        Task<(bool Success, string Message)> CreateReportAsync(Report report);
        Task<(bool Success, string Message)> UpdateReportAsync(Report report);
        Task<(bool Success, string Message)> GenerateAsync(object model);
    }
}
