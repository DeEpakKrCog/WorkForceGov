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
        private readonly IJobOpeningRepository _jobRepository;
        private readonly IProgramRepository _programRepository;
        private readonly ICitizenRepository _citizenRepository;
        private readonly IEmployerRepository _employerRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly IBenefitRepository _benefitRepository;
        private readonly ISystemLogRepository _logRepository;

        public AdminDashboardService(
            IRepository<User> userRepository,
            IUserRepository userRepo,
            IRepository<ComplianceRecord> complianceRepository,
            IJobOpeningRepository jobRepository,
            IProgramRepository programRepository,
            ICitizenRepository citizenRepository,
            IEmployerRepository employerRepository,
            IApplicationRepository applicationRepository,
            IAuditRepository auditRepository,
            IBenefitRepository benefitRepository,
            ISystemLogRepository logRepository)
        {
            _userRepository = userRepository;
            _userRepo = userRepo;
            _complianceRepository = complianceRepository;
            _jobRepository = jobRepository;
            _programRepository = programRepository;
            _citizenRepository = citizenRepository;
            _employerRepository = employerRepository;
            _applicationRepository = applicationRepository;
            _auditRepository = auditRepository;
            _benefitRepository = benefitRepository;
            _logRepository = logRepository;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var jobs = await _jobRepository.GetAllAsync();
            var programs = await _programRepository.GetAllAsync();
            var citizens = await _citizenRepository.GetAllAsync();
            var employers = await _employerRepository.GetAllAsync();
            var applications = await _applicationRepository.GetAllAsync();
            var audits = await _auditRepository.GetAllAsync();
            var benefits = await _benefitRepository.GetAllAsync();
            var logs = await _logRepository.GetAllAsync();

            return new DashboardViewModel
            {
                TotalUsers = users.Count(),
                TotalCitizens = citizens.Count(),
                TotalEmployers = employers.Count(),
                TotalJobs = jobs.Count(),
                OpenJobs = jobs.Count(j => j.Status == "Open"),
                TotalPrograms = programs.Count(),
                ActivePrograms = programs.Count(p => p.Status == "Active"),
                TotalApplications = applications.Count(),
                PendingApplications = applications.Count(a => a.Status == "Pending"),
                TotalAudits = audits.Count(),
                TotalBudget = benefits.Sum(b => b.Amount),
                TotalBenefitsPaid = benefits.Where(b => b.Status == "Paid").Sum(b => b.Amount),
                RecentJobs = jobs.OrderByDescending(j => j.PostedDate).Take(5).ToList(),
                RecentLogs = logs.OrderByDescending(l => l.Timestamp).Take(10).ToList(),
                Programs = programs.OrderByDescending(p => p.Id).Take(5).ToList(),
            };
        }
    }
}
