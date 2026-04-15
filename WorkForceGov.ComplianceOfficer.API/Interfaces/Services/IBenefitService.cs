using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IBenefitService
    {
        Task<IEnumerable<Benefit>> GetAllBenefitsAsync();
        Task<Benefit?> GetBenefitByIdAsync(int id);
        Task<IEnumerable<Benefit>> GetBenefitsByCitizenAsync(int citizenId);
        Task<IEnumerable<Benefit>> GetBenefitsByProgramAsync(int programId);
        Task<(bool Success, string Message)> CreateBenefitAsync(Benefit benefit);
        Task<(bool Success, string Message)> UpdateBenefitAsync(Benefit benefit);
        Task<IEnumerable<Benefit>> GetByCitizenAsync(int citizenId);
    }
}
