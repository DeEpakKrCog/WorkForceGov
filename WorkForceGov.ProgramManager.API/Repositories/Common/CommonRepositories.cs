using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Repositories.Common
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext ctx) : base(ctx) { }
        public async Task<User?> GetByEmailAsync(string email) =>
            await _set.FirstOrDefaultAsync(u => u.Email == email);
        public async Task<IEnumerable<User>> GetByRoleAsync(string role) =>
            await _set.Where(u => u.Role == role).ToListAsync();
    }

    public class SystemLogRepository : Repository<SystemLog>, ISystemLogRepository
    {
        public SystemLogRepository(ApplicationDbContext ctx) : base(ctx) { }
    }

    public class ProgramRepository : Repository<EmploymentProgram>, IProgramRepository
    {
        public ProgramRepository(ApplicationDbContext ctx) : base(ctx) { }
    }

    public class TrainingRepository : Repository<Training>, ITrainingRepository
    {
        public TrainingRepository(ApplicationDbContext ctx) : base(ctx) { }
    }

    public class TrainingEnrollmentRepository : Repository<TrainingEnrollment>, ITrainingEnrollmentRepository
    {
        public TrainingEnrollmentRepository(ApplicationDbContext ctx) : base(ctx) { }

        public async Task<IEnumerable<TrainingEnrollment>> GetByCitizenAsync(int citizenId) =>
            await _set.Include(e => e.Training).ThenInclude(t => t.Program)
                      .Where(e => e.CitizenId == citizenId)
                      .OrderByDescending(e => e.EnrolledDate)
                      .ToListAsync();

        public async Task<TrainingEnrollment?> GetByCitizenAndTrainingAsync(int citizenId, int trainingId) =>
            await _set.FirstOrDefaultAsync(e => e.CitizenId == citizenId && e.TrainingId == trainingId);
    }

    public class ResourceRepository : Repository<Resource>, IResourceRepository
    {
        public ResourceRepository(ApplicationDbContext ctx) : base(ctx) { }
    }
}
