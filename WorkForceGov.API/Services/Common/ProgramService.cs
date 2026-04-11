using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.Common
{
    public class ProgramService : IProgramService
    {
        private readonly IProgramRepository _programRepository;

        public ProgramService(IProgramRepository programRepository)
        {
            _programRepository = programRepository;
        }

        public async Task<IEnumerable<EmploymentProgram>> GetAllProgramsAsync()
        {
            return await _programRepository.GetAllAsync();
        }

        public async Task<IEnumerable<EmploymentProgram>> GetAllAsync()
        {
            return await _programRepository.GetAllAsync();
        }

        public async Task<EmploymentProgram?> GetProgramByIdAsync(int id)
        {
            return await _programRepository.GetByIdAsync(id);
        }

        public async Task<EmploymentProgram?> GetByIdAsync(int id)
        {
            return await _programRepository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> CreateProgramAsync(EmploymentProgram program)
        {
            try
            {
                await _programRepository.AddAsync(program);
                await _programRepository.SaveAsync();
                return (true, "Program created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating program: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CreateAsync(object model)
        {
            try
            {
                if (model is EmploymentProgram program)
                {
                    await _programRepository.AddAsync(program);
                    await _programRepository.SaveAsync();
                    return (true, "Program created successfully");
                }
                return (false, "Invalid program data");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating program: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateProgramAsync(EmploymentProgram program)
        {
            try
            {
                _programRepository.Update(program);
                await _programRepository.SaveAsync();
                return (true, "Program updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating program: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(object model)
        {
            try
            {
                if (model is EmploymentProgram program)
                {
                    _programRepository.Update(program);
                    await _programRepository.SaveAsync();
                    return (true, "Program updated successfully");
                }
                return (false, "Invalid program data");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating program: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var program = await _programRepository.GetByIdAsync(id);
                if (program == null)
                    return (false, "Program not found");

                _programRepository.Remove(program);
                await _programRepository.SaveAsync();
                return (true, "Program deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting program: {ex.Message}");
            }
        }

        public async Task<ProgramManagerDashboardViewModel> GetDashboardAsync(int userId)
        {
            var programs = (await GetAllAsync()).ToList();
            return new ProgramManagerDashboardViewModel
            {
                TotalPrograms = programs.Count,
                ActivePrograms = programs.Count(p => p.Status == "Active"),
                TotalBudget = programs.Sum(p => p.TotalBudget),
                BudgetUtilized = 0,
                TotalTrainings = 0,
                TotalBeneficiaries = 0,
                Programs = programs,
                ActiveTrainings = new List<Training>(),
                Notifications = new List<Notification>()
            };
        }
    }
}
