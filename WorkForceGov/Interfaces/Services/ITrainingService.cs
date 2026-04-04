using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface ITrainingService
    {
        Task<IEnumerable<Training>> GetAllAsync();
        Task<IEnumerable<Training>> GetAllTrainingsAsync();
        Task<Training?> GetByIdAsync(int id);
        Task<Training?> GetTrainingByIdAsync(int id);
        Task<IEnumerable<Training>> GetTrainingsByProgramAsync(int programId);
        Task<(bool Success, string Message)> CreateAsync(object model);
        Task<(bool Success, string Message)> CreateTrainingAsync(Training training);
        Task<(bool Success, string Message)> UpdateAsync(object model);
        Task<(bool Success, string Message)> UpdateTrainingAsync(Training training);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}
