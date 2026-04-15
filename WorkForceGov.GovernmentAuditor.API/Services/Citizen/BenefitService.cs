using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Citizen
{
    public class BenefitService : IBenefitService
    {
        private readonly IBenefitRepository _benefitRepository;

        public BenefitService(IBenefitRepository benefitRepository)
        {
            _benefitRepository = benefitRepository;
        }

        public async Task<IEnumerable<Benefit>> GetAllBenefitsAsync()
        {
            return await _benefitRepository.GetAllAsync();
        }

        public async Task<Benefit?> GetBenefitByIdAsync(int id)
        {
            return await _benefitRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Benefit>> GetBenefitsByCitizenAsync(int citizenId)
        {
            var benefits = await _benefitRepository.GetAllAsync();
            return benefits.Where(b => b.CitizenId == citizenId).ToList();
        }

        public async Task<IEnumerable<Benefit>> GetBenefitsByProgramAsync(int programId)
        {
            var benefits = await _benefitRepository.GetAllAsync();
            return benefits.Where(b => b.ProgramId == programId).ToList();
        }

        public async Task<(bool Success, string Message)> CreateBenefitAsync(Benefit benefit)
        {
            try
            {
                await _benefitRepository.AddAsync(benefit);
                await _benefitRepository.SaveAsync();
                return (true, "Benefit created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating benefit: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateBenefitAsync(Benefit benefit)
        {
            try
            {
                _benefitRepository.Update(benefit);
                await _benefitRepository.SaveAsync();
                return (true, "Benefit updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating benefit: {ex.Message}");
            }
        }

        public async Task<IEnumerable<Benefit>> GetByCitizenAsync(int citizenId)
        {
            return await GetBenefitsByCitizenAsync(citizenId);
        }
    }
}
