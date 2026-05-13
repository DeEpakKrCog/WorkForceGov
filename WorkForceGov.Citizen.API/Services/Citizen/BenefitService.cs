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

        // FIXED: Now uses the efficient database query that includes EmploymentProgram
        public async Task<IEnumerable<Benefit>> GetBenefitsByCitizenAsync(int citizenId)
        {
            return await _benefitRepository.GetByCitizenWithProgramAsync(citizenId);
        }

        // FIXED: Now uses the efficient database query
        public async Task<IEnumerable<Benefit>> GetBenefitsByProgramAsync(int programId)
        {
            return await _benefitRepository.GetByProgramAsync(programId);
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

        // ADDED: The missing Apply Logic
        public async Task<(bool Success, string Message)> ApplyAsync(int citizenId, int programId)
        {
            try
            {
                var existing = await _benefitRepository.GetByCitizenWithProgramAsync(citizenId);
                if (existing.Any(b => b.ProgramId == programId))
                {
                    return (false, "You have already applied for this program.");
                }

                var newBenefit = new Benefit
                {
                    CitizenId = citizenId,
                    ProgramId = programId,
                    Status = "Pending",
                    BenefitDate = DateTime.UtcNow,
                    BenefitType = "Application",
                    Amount = 0
                };

                await _benefitRepository.AddAsync(newBenefit);
                await _benefitRepository.SaveAsync();
                return (true, "Application submitted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error applying for program: {ex.Message}");
            }
        }
    }
}