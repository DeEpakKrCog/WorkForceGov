using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.LaborOfficer
{
    public class ComplianceService : IComplianceService
    {
        private readonly IComplianceRecordRepository _complianceRepository;

        public ComplianceService(IComplianceRecordRepository complianceRepository)
        {
            _complianceRepository = complianceRepository;
        }

        public async Task<IEnumerable<ComplianceRecord>> GetAllComplianceRecordsAsync()
        {
            return await _complianceRepository.GetAllAsync();
        }

        public async Task<IEnumerable<ComplianceRecord>> GetAllAsync()
        {
            return await _complianceRepository.GetAllAsync();
        }

        public async Task<ComplianceRecord?> GetComplianceRecordByIdAsync(int id)
        {
            return await _complianceRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<ComplianceRecord>> GetComplianceRecordsByEntityAsync(int entityId, string type)
        {
            var records = await _complianceRepository.GetAllAsync();
            return records.Where(r => r.EntityId == entityId && r.Type == type).ToList();
        }

        public async Task<(bool Success, string Message)> CreateComplianceRecordAsync(ComplianceRecord record)
        {
            try
            {
                await _complianceRepository.AddAsync(record);
                await _complianceRepository.SaveAsync();
                return (true, "Compliance record created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating compliance record: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateComplianceRecordAsync(ComplianceRecord record)
        {
            try
            {
                _complianceRepository.Update(record);
                await _complianceRepository.SaveAsync();
                return (true, "Compliance record updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating compliance record: {ex.Message}");
            }
        }

        public async Task<object> GetDashboardAsync(int userId)
        {
            // Placeholder for dashboard logic
            var records = await GetAllAsync();
            return new { TotalRecords = records.Count() };
        }

        public async Task CreateAsync(ComplianceRecord record)
        {
            await _complianceRepository.AddAsync(record);
            await _complianceRepository.SaveAsync();
        }
    }
}
