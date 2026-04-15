using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Models;
using WorkForceGovProject.Repositories.Common;

namespace WorkForceGovProject.Repositories.Employer
{
    public class EmployerRepository : Repository<Models.Employer>, IEmployerRepository
    {
        public EmployerRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<Models.Employer?> GetByUserIdAsync(int userId) =>
            await _set.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);

        public async Task<Models.Employer?> GetWithJobsAsync(int employerId) =>
            await _set.Include(e => e.JobOpenings).FirstOrDefaultAsync(e => e.Id == employerId);

        public async Task<Models.Employer?> GetWithDocumentsAsync(int employerId) =>
            await _set.Include(e => e.Documents).FirstOrDefaultAsync(e => e.Id == employerId);

        public async Task<IEnumerable<Models.Employer>> GetByStatusAsync(string status) =>
            await _set.Where(e => e.Status == status).ToListAsync();
    }

    public class EmployerDocumentRepository : Repository<EmployerDocument>, IEmployerDocumentRepository
    {
        public EmployerDocumentRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<EmployerDocument>> GetByEmployerAsync(int employerId) =>
            await _set.Where(d => d.EmployerId == employerId).OrderByDescending(d => d.UploadedDate).ToListAsync();

        public async Task<IEnumerable<EmployerDocument>> GetPendingVerificationsAsync() =>
            await _set.Include(d => d.Employer).Where(d => d.VerificationStatus == "Pending")
                      .OrderBy(d => d.UploadedDate).ToListAsync();

        public async Task<IEnumerable<EmployerDocument>> GetPendingAsync() =>
            await GetPendingVerificationsAsync();
    }

    public class JobOpeningRepository : Repository<JobOpening>, IJobOpeningRepository
    {
        public JobOpeningRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<JobOpening>> GetOpenJobsAsync() =>
            await _set.Include(j => j.Employer).Where(j => j.Status == "Open")
                      .OrderByDescending(j => j.PostedDate).ToListAsync();

        public async Task<IEnumerable<JobOpening>> SearchAsync(string? keyword, string? location, string? category)
        {
            var q = _set.Include(j => j.Employer).Where(j => j.Status == "Open").AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(j => j.JobTitle.Contains(keyword) || j.Description.Contains(keyword));
            if (!string.IsNullOrWhiteSpace(location))
                q = q.Where(j => j.Location.Contains(location));
            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(j => j.JobCategory != null && j.JobCategory.Contains(category));
            return await q.OrderByDescending(j => j.PostedDate).ToListAsync();
        }

        public async Task<JobOpening?> GetWithApplicationsAsync(int id) =>
            await _set.Include(j => j.Applications).ThenInclude(a => a.Citizen)
                      .Include(j => j.Employer).FirstOrDefaultAsync(j => j.Id == id);

        public async Task<IEnumerable<JobOpening>> GetByEmployerAsync(int employerId) =>
            await _set.Where(j => j.EmployerId == employerId).Include(j => j.Applications)
                      .OrderByDescending(j => j.PostedDate).ToListAsync();
    }
}
