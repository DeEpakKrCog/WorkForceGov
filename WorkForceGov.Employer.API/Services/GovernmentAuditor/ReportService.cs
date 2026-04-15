using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.GovernmentAuditor
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<IEnumerable<Report>> GetAllReportsAsync()
        {
            return await _reportRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Report>> GetAllAsync()
        {
            return await _reportRepository.GetAllAsync();
        }

        public async Task<Report?> GetReportByIdAsync(int id)
        {
            return await _reportRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Report>> GetReportsByUserAsync(int userId)
        {
            var reports = await _reportRepository.GetAllAsync();
            return reports.Where(r => r.GeneratedBy == userId).ToList();
        }

        public async Task<(bool Success, string Message)> CreateReportAsync(Report report)
        {
            try
            {
                await _reportRepository.AddAsync(report);
                await _reportRepository.SaveAsync();
                return (true, "Report created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating report: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateReportAsync(Report report)
        {
            try
            {
                _reportRepository.Update(report);
                await _reportRepository.SaveAsync();
                return (true, "Report updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating report: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> GenerateAsync(object model)
        {
            try
            {
                if (model is Report report)
                {
                    report.GeneratedDate = DateTime.Now;
                    await _reportRepository.AddAsync(report);
                    await _reportRepository.SaveAsync();
                    return (true, "Report generated successfully");
                }
                return (false, "Invalid report data");
            }
            catch (Exception ex)
            {
                return (false, $"Error generating report: {ex.Message}");
            }
        }
    }
}
