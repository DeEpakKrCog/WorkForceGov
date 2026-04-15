using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Repositories.ProgramManager
{
    /// <summary>
    /// Low-level data access for Program Manager-specific cross-table operations.
    /// Provides benefit, training, and program queries with required navigation
    /// properties pre-loaded for PM views.
    /// </summary>
    public class ProgramManagerRepository
    {
        private readonly ApplicationDbContext _ctx;

        public ProgramManagerRepository(ApplicationDbContext ctx)
        {
            _ctx = ctx;
        }

        // ── BENEFITS ─────────────────────────────────────────────────────────

        public async Task<IEnumerable<Benefit>> GetPendingBenefitsAsync() =>
            await _ctx.Benefits
                      .Include(b => b.Citizen).ThenInclude(c => c.User)
                      .Include(b => b.Program)
                      .Where(b => b.Status == "Pending")
                      .OrderBy(b => b.BenefitDate)
                      .ToListAsync();

        public async Task<IEnumerable<Benefit>> GetAllBenefitsAsync() =>
            await _ctx.Benefits
                      .Include(b => b.Citizen).ThenInclude(c => c.User)
                      .Include(b => b.Program)
                      .OrderByDescending(b => b.BenefitDate)
                      .ToListAsync();

        public async Task<Benefit?> GetBenefitByIdAsync(int id) =>
            await _ctx.Benefits
                      .Include(b => b.Citizen).ThenInclude(c => c.User)
                      .Include(b => b.Program)
                      .FirstOrDefaultAsync(b => b.Id == id);

        public async Task UpdateBenefitAsync(Benefit benefit)
        {
            _ctx.Benefits.Update(benefit);
            await _ctx.SaveChangesAsync();
        }

        // ── PROGRAMS ─────────────────────────────────────────────────────────

        public async Task<IEnumerable<EmploymentProgram>> GetAllProgramsAsync() =>
            await _ctx.EmploymentPrograms
                      .Include(p => p.Trainings)
                      .Include(p => p.Benefits)
                      .OrderByDescending(p => p.StartDate)
                      .ToListAsync();

        // ── TRAININGS ────────────────────────────────────────────────────────

        public async Task<IEnumerable<Training>> GetActiveTrainingsAsync() =>
            await _ctx.Trainings
                      .Include(t => t.Program)
                      .Include(t => t.Enrollments)
                      .Where(t => t.Status == "Active")
                      .OrderBy(t => t.StartDate)
                      .ToListAsync();

        // ── METRICS ──────────────────────────────────────────────────────────

        public async Task<int> GetTotalBeneficiariesAsync() =>
            await _ctx.Benefits
                      .Select(b => b.CitizenId)
                      .Distinct()
                      .CountAsync();

        public async Task<decimal> GetTotalBudgetUtilizedAsync() =>
            await _ctx.Benefits
                      .Where(b => b.Status == "Active" || b.Status == "Paid")
                      .SumAsync(b => b.Amount);
    }
}
