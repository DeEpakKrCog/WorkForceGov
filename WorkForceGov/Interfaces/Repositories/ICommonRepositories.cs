using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Repositories
{
    /// <summary>
    /// Data access for formal audit records conducted by Government Auditors.
    /// </summary>
    public interface IAuditRepository : IRepository<Audit>
    {
        Task<IEnumerable<Audit>> GetByOfficerAsync(int officerId);
        Task<IEnumerable<Audit>> GetByScopeAsync(string scope);
        Task<IEnumerable<Audit>> GetByStatusAsync(string status);
    }

    /// <summary>
    /// Data access for system-generated reports.
    /// </summary>
    public interface IReportRepository : IRepository<Report> { }

    /// <summary>
    /// Data access for user accounts (shared across all modules).
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetByRoleAsync(string role);
    }

    /// <summary>
    /// Data access for system activity logs.
    /// </summary>
    public interface ISystemLogRepository : IRepository<SystemLog> { }

    /// <summary>
    /// Data access for training enrollments by citizens.
    /// </summary>
    public interface ITrainingEnrollmentRepository : IRepository<TrainingEnrollment>
    {
        Task<IEnumerable<TrainingEnrollment>> GetByCitizenAsync(int citizenId);
        Task<TrainingEnrollment?> GetByCitizenAndTrainingAsync(int citizenId, int trainingId);
    }

    /// <summary>
    /// Data access for workforce employment programs.
    /// </summary>
    public interface IProgramRepository : IRepository<EmploymentProgram> { }

    /// <summary>
    /// Data access for training sessions within programs.
    /// </summary>
    public interface ITrainingRepository : IRepository<Training> { }

    /// <summary>
    /// Data access for program resources.
    /// </summary>
    public interface IResourceRepository : IRepository<Resource> { }
}
