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

    public class ResourceRepository : Repository<Resource>, IResourceRepository
    {
        public ResourceRepository(ApplicationDbContext ctx) : base(ctx) { }
    }
}
