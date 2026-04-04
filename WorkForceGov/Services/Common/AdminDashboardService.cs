using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.Common
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUserRepository _userRepo;
        private readonly IRepository<ComplianceRecord> _complianceRepository;

        public AdminDashboardService(IRepository<User> userRepository, IUserRepository userRepo, IRepository<ComplianceRecord> complianceRepository)
        {
            _userRepository = userRepository;
            _userRepo = userRepo;
            _complianceRepository = complianceRepository;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var compliances = await _complianceRepository.GetAllAsync();

            return new AdminDashboardViewModel
            {
                TotalUsers = users.Count(),
                TotalComplianceRecords = compliances.Count()
            };
        }
    }
}
