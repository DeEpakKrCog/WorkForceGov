using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;

namespace WorkForceGovProject.Repositories.Common
{
    /// <summary>
    /// Generic Repository — provides standard CRUD for any entity.
    /// All module-specific repositories inherit from this.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _ctx;
        protected readonly DbSet<T> _set;

        public Repository(ApplicationDbContext ctx)
        {
            _ctx = ctx;
            _set = ctx.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);
        public async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> p) => await _set.Where(p).ToListAsync();
        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> p) => await _set.FirstOrDefaultAsync(p);
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> p) => await _set.AnyAsync(p);
        public async Task<int> CountAsync(Expression<Func<T, bool>>? p = null) =>
            p == null ? await _set.CountAsync() : await _set.CountAsync(p);
        public async Task AddAsync(T entity) => await _set.AddAsync(entity);
        public void Update(T entity) => _set.Update(entity);
        public void Remove(T entity) => _set.Remove(entity);
        public async Task SaveAsync() => await _ctx.SaveChangesAsync();
        public IQueryable<T> Query() => _set.AsQueryable();
    }
}
