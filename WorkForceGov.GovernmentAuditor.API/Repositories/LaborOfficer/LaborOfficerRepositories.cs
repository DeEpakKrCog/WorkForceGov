using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Models;
using WorkForceGovProject.Repositories.Common;

namespace WorkForceGovProject.Repositories.LaborOfficer
{
    public class ComplianceRecordRepository : Repository<ComplianceRecord>, IComplianceRecordRepository
    {
        public ComplianceRecordRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<ComplianceRecord>> GetByEntityAsync(int entityId, string type) =>
            await _set.Include(c => c.Officer)
                      .Where(c => c.EntityId == entityId && c.Type == type)
                      .OrderByDescending(c => c.Date).ToListAsync();

        public async Task<IEnumerable<ComplianceRecord>> GetByOfficerAsync(int officerId) =>
            await _set.Where(c => c.OfficerId == officerId)
                      .OrderByDescending(c => c.Date).ToListAsync();

        public async Task<IEnumerable<ComplianceRecord>> GetPendingReviewsAsync() =>
            await _set.Include(c => c.Officer)
                      .Where(c => c.Result == "Under Review")
                      .OrderBy(c => c.Date).ToListAsync();
    }

    public class ViolationRepository : Repository<Violation>, IViolationRepository
    {
        public ViolationRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<Violation>> GetByEmployerAsync(int employerId) =>
            await _set.Include(v => v.Employer).Include(v => v.Officer)
                      .Where(v => v.EmployerId == employerId)
                      .OrderByDescending(v => v.ViolationDate).ToListAsync();

        public async Task<IEnumerable<Violation>> GetByOfficerAsync(int officerId) =>
            await _set.Include(v => v.Employer)
                      .Where(v => v.OfficerId == officerId)
                      .OrderByDescending(v => v.ViolationDate).ToListAsync();
    }

    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<Notification>> GetByUserAsync(int userId, int count = 20) =>
            await _set.Where(n => n.UserId == userId)
                      .OrderByDescending(n => n.CreatedDate).Take(count).ToListAsync();

        public async Task<int> GetUnreadCountAsync(int userId) =>
            await _set.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task MarkAllReadAsync(int userId)
        {
            var unread = await _set.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var n in unread) n.IsRead = true;
            await _ctx.SaveChangesAsync();
        }
    }
}
