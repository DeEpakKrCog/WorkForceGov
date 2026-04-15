using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Models;
using WorkForceGovProject.Repositories.Common;

namespace WorkForceGovProject.Repositories.GovernmentAuditor
{
    public class AuditRepository : Repository<Audit>, IAuditRepository
    {
        public AuditRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<Audit>> GetByOfficerAsync(int officerId) =>
            await _set.Include(a => a.Officer)
                      .Where(a => a.OfficerId == officerId)
                      .OrderByDescending(a => a.Date).ToListAsync();

        public async Task<IEnumerable<Audit>> GetByScopeAsync(string scope) =>
            await _set.Include(a => a.Officer)
                      .Where(a => a.Scope == scope)
                      .OrderByDescending(a => a.Date).ToListAsync();

        public async Task<IEnumerable<Audit>> GetByStatusAsync(string status) =>
            await _set.Include(a => a.Officer)
                      .Where(a => a.Status == status)
                      .OrderByDescending(a => a.Date).ToListAsync();
    }

    public class ReportRepository : Repository<Report>, IReportRepository
    {
        public ReportRepository(ApplicationDbContext ctx) : base(ctx) { }
    }
}
