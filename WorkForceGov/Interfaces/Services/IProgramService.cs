using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IProgramService
    {
        Task<IEnumerable<EmploymentProgram>> GetAllProgramsAsync();
        Task<IEnumerable<EmploymentProgram>> GetAllAsync();
        Task<EmploymentProgram?> GetProgramByIdAsync(int id);
        Task<EmploymentProgram?> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CreateProgramAsync(EmploymentProgram program);
        Task<(bool Success, string Message)> CreateAsync(object model);
        Task<(bool Success, string Message)> UpdateProgramAsync(EmploymentProgram program);
        Task<(bool Success, string Message)> UpdateAsync(object model);
        Task<(bool Success, string Message)> DeleteAsync(int id);
        Task<ProgramManagerDashboardViewModel> GetDashboardAsync(int userId);
    }
}
