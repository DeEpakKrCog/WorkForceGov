using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Models;
using WorkForceGovProject.Repositories.Common;

namespace WorkForceGovProject.Repositories.Citizen
{
    public class CitizenRepository : Repository<Models.Citizen>, ICitizenRepository
    {
        public CitizenRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<Models.Citizen?> GetByUserIdAsync(int userId) =>
            await _set.Include(c => c.User).FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task<Models.Citizen?> GetWithDocumentsAsync(int citizenId) =>
            await _set.Include(c => c.Documents).FirstOrDefaultAsync(c => c.Id == citizenId);

        public async Task<Models.Citizen?> GetWithApplicationsAsync(int citizenId) =>
            await _set.Include(c => c.Applications).ThenInclude(a => a.JobOpening)
                      .ThenInclude(j => j.Employer).FirstOrDefaultAsync(c => c.Id == citizenId);

        public async Task<Models.Citizen?> GetWithBenefitsAsync(int citizenId) =>
            await _set.Include(c => c.Benefits).ThenInclude(b => b.Program)
                      .FirstOrDefaultAsync(c => c.Id == citizenId);
    }

    public class CitizenDocumentRepository : Repository<CitizenDocument>, ICitizenDocumentRepository
    {
        public CitizenDocumentRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<CitizenDocument>> GetByCitizenAsync(int citizenId) =>
            await _set.Where(d => d.CitizenId == citizenId).OrderByDescending(d => d.UploadedDate).ToListAsync();

        public async Task<IEnumerable<CitizenDocument>> GetPendingVerificationsAsync() =>
            await _set.Include(d => d.Citizen).Where(d => d.VerificationStatus == "Pending")
                      .OrderBy(d => d.UploadedDate).ToListAsync();
    }

    public class ApplicationRepository : Repository<Application>, IApplicationRepository
    {
        public ApplicationRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<Application>> GetByCitizenAsync(int citizenId) =>
            await _set.Include(a => a.JobOpening).ThenInclude(j => j.Employer)
                      .Where(a => a.CitizenId == citizenId).OrderByDescending(a => a.SubmittedDate).ToListAsync();

        public async Task<IEnumerable<Application>> GetByJobAsync(int jobId) =>
            await _set.Include(a => a.Citizen).Where(a => a.JobOpeningId == jobId).ToListAsync();

        public async Task<IEnumerable<Application>> GetByEmployerAsync(int employerId) =>
            await _set.Include(a => a.Citizen).Include(a => a.JobOpening)
                      .Where(a => a.JobOpening.EmployerId == employerId)
                      .OrderByDescending(a => a.SubmittedDate).ToListAsync();

        public async Task<Application?> GetWithDetailsAsync(int id) =>
            await _set.Include(a => a.Citizen).Include(a => a.JobOpening).ThenInclude(j => j.Employer)
                      .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<bool> HasAppliedAsync(int citizenId, int jobId) =>
            await _set.AnyAsync(a => a.CitizenId == citizenId && a.JobOpeningId == jobId);

        public async Task<IEnumerable<Application>> GetAllWithDetailsAsync() =>
            await _set.Include(a => a.Citizen)
                      .Include(a => a.JobOpening).ThenInclude(j => j.Employer)
                      .OrderByDescending(a => a.SubmittedDate).ToListAsync();
    }

    public class ComplaintRepository : Repository<Complaint>, IComplaintRepository
    {
        public ComplaintRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<Complaint>> GetByCitizenUserIdAsync(int userId) =>
            await _set.Include(c => c.Employer).Where(c => c.UserId == userId)
                      .OrderByDescending(c => c.SubmittedDate).ToListAsync();

        public async Task<IEnumerable<Complaint>> GetByEmployerAsync(int employerId) =>
            await _set.Include(c => c.User).Where(c => c.EmployerId == employerId).ToListAsync();

        public async Task<IEnumerable<Complaint>> GetByStatusAsync(string status) =>
            await _set.Include(c => c.User).Include(c => c.Employer)
                      .Where(c => c.Status == status).ToListAsync();

        public async Task<IEnumerable<Complaint>> GetPendingComplaintsAsync() =>
            await _set.Include(c => c.User).Include(c => c.Employer)
                      .Where(c => c.Status == "Pending").OrderBy(c => c.SubmittedDate).ToListAsync();
    }

    public class BenefitRepository : Repository<Benefit>, IBenefitRepository
    {
        public BenefitRepository(ApplicationDbContext ctx) : base(ctx) { }
    }
}
