using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Repositories
{
    /// <summary>
    /// Data access for Citizen profiles.
    /// </summary>
    public interface ICitizenRepository : IRepository<Citizen>
    {
        Task<Citizen?> GetByUserIdAsync(int userId);
        Task<Citizen?> GetWithDocumentsAsync(int citizenId);
        Task<Citizen?> GetWithApplicationsAsync(int citizenId);
        Task<Citizen?> GetWithBenefitsAsync(int citizenId);
    }

    /// <summary>
    /// Data access for citizen-uploaded verification documents.
    /// </summary>
    public interface ICitizenDocumentRepository : IRepository<CitizenDocument>
    {
        Task<IEnumerable<CitizenDocument>> GetByCitizenAsync(int citizenId);
        Task<IEnumerable<CitizenDocument>> GetPendingVerificationsAsync();
    }

    /// <summary>
    /// Data access for job applications submitted by citizens.
    /// </summary>
    public interface IApplicationRepository : IRepository<Application>
    {
        Task<IEnumerable<Application>> GetByCitizenAsync(int citizenId);
        Task<IEnumerable<Application>> GetByJobAsync(int jobId);
        Task<IEnumerable<Application>> GetByEmployerAsync(int employerId);
        Task<Application?> GetWithDetailsAsync(int id);
        Task<bool> HasAppliedAsync(int citizenId, int jobId);
        Task<IEnumerable<Application>> GetAllWithDetailsAsync();
    }

    /// <summary>
    /// Data access for citizen-raised complaints against employers.
    /// Cross-module: complaints raised here surface in the Labor Officer module.
    /// </summary>
    public interface IComplaintRepository : IRepository<Complaint>
    {
        Task<IEnumerable<Complaint>> GetByCitizenUserIdAsync(int userId);
        Task<IEnumerable<Complaint>> GetByEmployerAsync(int employerId);
        Task<IEnumerable<Complaint>> GetByStatusAsync(string status);
        Task<IEnumerable<Complaint>> GetPendingComplaintsAsync();
    }

    /// <summary>
    /// Data access for benefits allocated to citizens.
    /// </summary>
    public interface IBenefitRepository : IRepository<Benefit> { }
}
