using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    /// <summary>
    /// Consolidated Admin service: employer oversight, notification broadcasting,
    /// and platform-wide statistics. User CRUD is handled via IAccountService.
    /// </summary>
    public interface IAdminService
    {
        // ── EMPLOYER OVERSIGHT ───────────────────────────────────────────────
        /// <summary>Returns all employers, optionally filtered by status (Pending/Verified/Suspended).</summary>
        Task<IEnumerable<Employer>> GetAllEmployersAsync(string? status = null);

        /// <summary>Suspends an employer and records the reason in their profile.</summary>
        Task<(bool Success, string Message)> SuspendEmployerAsync(int employerId, string reason);

        /// <summary>Reinstates a previously suspended employer to Active/Verified.</summary>
        Task<(bool Success, string Message)> ReinstateEmployerAsync(int employerId);

        // ── NOTIFICATIONS ────────────────────────────────────────────────────
        /// <summary>
        /// Broadcasts a message to every user matching <paramref name="targetRole"/>,
        /// or to all users when targetRole is "All".
        /// </summary>
        Task<(bool Success, string Message)> BroadcastNotificationAsync(
            string targetRole, string message, INotificationService notificationService);

        // ── STATISTICS / DASHBOARD ───────────────────────────────────────────
        /// <summary>Returns the full admin dashboard view-model.</summary>
        Task<DashboardViewModel> GetFullDashboardAsync();
    }
}
