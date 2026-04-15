using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    /// <summary>
    /// Dedicated Program Manager service: benefit approval workflow,
    /// program performance metrics, and training enrollment summaries.
    /// </summary>
    public interface IProgramManagerService
    {
        // ── BENEFIT WORKFLOW ─────────────────────────────────────────────────
        /// <summary>Returns all benefits with status "Pending" awaiting PM review.</summary>
        Task<IEnumerable<Benefit>> GetPendingBenefitsAsync();

        /// <summary>
        /// Approves a pending benefit, sets its amount, and sends a notification
        /// to the citizen.
        /// </summary>
        Task<(bool Success, string Message)> ApproveBenefitAsync(
            int benefitId, int managerUserId, decimal amount,
            INotificationService notificationService);

        /// <summary>
        /// Rejects a pending benefit with a reason and notifies the citizen.
        /// </summary>
        Task<(bool Success, string Message)> RejectBenefitAsync(
            int benefitId, int managerUserId, string reason,
            INotificationService notificationService);

        // ── DASHBOARD / METRICS ──────────────────────────────────────────────
        /// <summary>
        /// Returns the enriched Program Manager dashboard including real training
        /// counts, beneficiary counts, and budget utilization.
        /// </summary>
        Task<ProgramManagerDashboardViewModel> GetEnrichedDashboardAsync(int userId);
    }
}
