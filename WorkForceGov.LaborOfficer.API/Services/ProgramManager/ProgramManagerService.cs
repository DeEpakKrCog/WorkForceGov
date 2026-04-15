using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;
using WorkForceGovProject.Repositories.ProgramManager;

namespace WorkForceGovProject.Services.ProgramManager
{
    /// <summary>
    /// Implements IProgramManagerService: benefit approval workflow,
    /// program/training metrics, and the enriched PM dashboard.
    /// </summary>
    public class ProgramManagerService : IProgramManagerService
    {
        private readonly ProgramManagerRepository _pmRepo;

        public ProgramManagerService(ProgramManagerRepository pmRepo)
        {
            _pmRepo = pmRepo;
        }

        // ── BENEFIT WORKFLOW ─────────────────────────────────────────────────

        public async Task<IEnumerable<Benefit>> GetPendingBenefitsAsync()
            => await _pmRepo.GetPendingBenefitsAsync();

        public async Task<(bool Success, string Message)> ApproveBenefitAsync(
            int benefitId, int managerUserId, decimal amount,
            INotificationService notificationService)
        {
            try
            {
                var benefit = await _pmRepo.GetBenefitByIdAsync(benefitId);
                if (benefit == null) return (false, "Benefit record not found.");
                if (benefit.Status != "Pending")
                    return (false, $"Benefit is already '{benefit.Status}' and cannot be approved.");
                if (amount <= 0)
                    return (false, "Approved amount must be greater than zero.");

                benefit.Status = "Active";
                benefit.Amount = amount;
                benefit.Description = $"Approved by manager (User #{managerUserId}) on {DateTime.Now:yyyy-MM-dd}.";
                await _pmRepo.UpdateBenefitAsync(benefit);

                // Notify the citizen
                await notificationService.CreateAsync(
                    benefit.Citizen.UserId,
                    $"Your benefit application for program '{benefit.Program.ProgramName}' has been approved. Amount: ${amount:N2}.",
                    "Benefit", benefit.Id, "Benefit");

                return (true, $"Benefit #{benefitId} approved with amount ${amount:N2}.");
            }
            catch (Exception ex)
            {
                return (false, $"Approval failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectBenefitAsync(
            int benefitId, int managerUserId, string reason,
            INotificationService notificationService)
        {
            try
            {
                var benefit = await _pmRepo.GetBenefitByIdAsync(benefitId);
                if (benefit == null) return (false, "Benefit record not found.");
                if (benefit.Status != "Pending")
                    return (false, $"Benefit is already '{benefit.Status}' and cannot be rejected.");

                benefit.Status = "Rejected";
                benefit.Description = $"Rejected by manager (User #{managerUserId}): {reason} — {DateTime.Now:yyyy-MM-dd}.";
                await _pmRepo.UpdateBenefitAsync(benefit);

                // Notify the citizen
                await notificationService.CreateAsync(
                    benefit.Citizen.UserId,
                    $"Your benefit application for program '{benefit.Program.ProgramName}' was rejected. Reason: {reason}",
                    "Benefit", benefit.Id, "Benefit");

                return (true, $"Benefit #{benefitId} has been rejected.");
            }
            catch (Exception ex)
            {
                return (false, $"Rejection failed: {ex.Message}");
            }
        }

        // ── ENRICHED DASHBOARD ───────────────────────────────────────────────

        public async Task<ProgramManagerDashboardViewModel> GetEnrichedDashboardAsync(int userId)
        {
            var programs        = (await _pmRepo.GetAllProgramsAsync()).ToList();
            var activeTrainings = (await _pmRepo.GetActiveTrainingsAsync()).ToList();
            var beneficiaries   = await _pmRepo.GetTotalBeneficiariesAsync();
            var budgetUtilized  = await _pmRepo.GetTotalBudgetUtilizedAsync();

            return new ProgramManagerDashboardViewModel
            {
                TotalPrograms     = programs.Count,
                ActivePrograms    = programs.Count(p => p.Status == "Active"),
                TotalBudget       = programs.Sum(p => p.TotalBudget),
                BudgetUtilized    = budgetUtilized,
                TotalTrainings    = activeTrainings.Count,
                TotalBeneficiaries= beneficiaries,
                Programs          = programs.Take(10).ToList(),
                ActiveTrainings   = activeTrainings.Take(5).ToList(),
                Notifications     = new List<Notification>()
            };
        }
    }
}
