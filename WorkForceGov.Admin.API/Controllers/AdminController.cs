using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _admin;
        private readonly IAccountService _account;
        private readonly IAdminDashboardService _dashboard;
        private readonly IReportService _reports;
        private readonly ISystemLogService _logs;
        private readonly INotificationService _notifications;

        public AdminController(IAdminService admin, IAccountService account,
            IAdminDashboardService dashboard, IReportService reports,
            ISystemLogService logs, INotificationService notifications)
        {
            _admin = admin; _account = account; _dashboard = dashboard;
            _reports = reports; _logs = logs; _notifications = notifications;
        }

        private int GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier);
            if (c != null && int.TryParse(c.Value, out int j)) return j;
            if (Request.Headers.TryGetValue("X-User-Id", out var h) && int.TryParse(h, out int p)) return p;
            throw new UnauthorizedAccessException("Provide a valid JWT or X-User-Id header.");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  DASHBOARD
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Returns the platform-wide admin dashboard statistics.</summary>
        [HttpGet("dashboard")]
        [SwaggerOperation(Summary = "Get admin dashboard", Tags = new[] { "Dashboard" })]
        public async Task<IActionResult> GetDashboard()
        {
            var model = await _admin.GetFullDashboardAsync();
            return Ok(model);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  USER MANAGEMENT  (FIX: DeactivateUser was calling DeleteUser)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Returns all users in the system.</summary>
        [HttpGet("users")]
        [SwaggerOperation(Summary = "Get all users", Tags = new[] { "User Management" })]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _account.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>Returns a single user by ID.</summary>
        [HttpGet("users/{id}")]
        [SwaggerOperation(Summary = "Get user by ID", Tags = new[] { "User Management" })]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _account.GetByIdAsync(id);
            if (user == null) return NotFound(new { Message = "User not found." });
            return Ok(user);
        }

        /// <summary>Creates a new system user.</summary>
        [HttpPost("users")]
        [SwaggerOperation(Summary = "Create a user", Tags = new[] { "User Management" })]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserViewModel model)
        {
            var (success, msg) = await _account.CreateUserAsync(model);
            if (success) return Ok(new { Message = msg });
            return BadRequest(new { Message = msg });
        }

        /// <summary>Updates an existing user's details.</summary>
        [HttpPut("users/{id}")]
        [SwaggerOperation(Summary = "Update a user", Tags = new[] { "User Management" })]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User model)
        {
            var existing = await _account.GetByIdAsync(id);
            if (existing == null) return NotFound(new { Message = "User not found." });
            existing.FullName = model.FullName;
            existing.Email = model.Email;
            existing.Role = model.Role;
            existing.Status = model.Status;
            existing.Phone = model.Phone;
            if (!string.IsNullOrEmpty(model.Password)) existing.Password = model.Password;
            var (success, msg) = await _account.UpdateUserAsync(existing);
            if (success) return Ok(new { Message = "User updated.", User = existing });
            return BadRequest(new { Message = msg });
        }

        /// <summary>
        /// Deactivates a user — sets Status to "Inactive". Does NOT delete the user.
        /// BUG FIX: Previously this was wired to DeleteUser in the MVC view.
        /// </summary>
        [HttpPut("users/{id}/deactivate")]
        [SwaggerOperation(Summary = "Deactivate a user (sets Inactive, keeps record)", Tags = new[] { "User Management" })]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var (success, msg) = await _account.DeactivateUserAsync(id);
            if (success) return Ok(new { Message = msg });
            return BadRequest(new { Message = msg });
        }

        /// <summary>Reactivates a previously deactivated user.</summary>
        [HttpPut("users/{id}/activate")]
        [SwaggerOperation(Summary = "Activate a deactivated user", Tags = new[] { "User Management" })]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var user = await _account.GetByIdAsync(id);
            if (user == null) return NotFound(new { Message = "User not found." });
            user.Status = "Active";
            var (success, msg) = await _account.UpdateUserAsync(user);
            if (success) return Ok(new { Message = $"User '{user.FullName}' activated." });
            return BadRequest(new { Message = msg });
        }

        /// <summary>Permanently deletes a user from the system.</summary>
        [HttpDelete("users/{id}")]
        [SwaggerOperation(Summary = "Permanently delete a user", Tags = new[] { "User Management" })]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var (success, msg) = await _account.DeleteUserAsync(id);
            if (success) return Ok(new { Message = msg });
            return BadRequest(new { Message = msg });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  EMPLOYER OVERSIGHT
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Returns all employers, optionally filtered by status (Pending/Verified/Suspended).</summary>
        [HttpGet("employers")]
        [SwaggerOperation(Summary = "Get all employers", Tags = new[] { "Employer Oversight" })]
        public async Task<IActionResult> GetEmployers([FromQuery] string? status)
        {
            var employers = await _admin.GetAllEmployersAsync(status);
            return Ok(employers);
        }

        /// <summary>Suspends an employer and records the reason.</summary>
        [HttpPut("employers/{id}/suspend")]
        [SwaggerOperation(Summary = "Suspend an employer", Tags = new[] { "Employer Oversight" })]
        public async Task<IActionResult> SuspendEmployer(int id, [FromBody] string reason)
        {
            var (success, msg) = await _admin.SuspendEmployerAsync(id, reason);
            if (success) await _logs.LogAsync(GetUserId(), "SuspendEmployer", "EmployerId=" + id);
            if (success) return Ok(new { Message = msg });
            return BadRequest(new { Message = msg });
        }

        /// <summary>Reinstates a suspended employer.</summary>
        [HttpPut("employers/{id}/reinstate")]
        [SwaggerOperation(Summary = "Reinstate a suspended employer", Tags = new[] { "Employer Oversight" })]
        public async Task<IActionResult> ReinstateEmployer(int id)
        {
            var (success, msg) = await _admin.ReinstateEmployerAsync(id);
            if (success) await _logs.LogAsync(GetUserId(), "ReinstateEmployer", "EmployerId=" + id);
            if (success) return Ok(new { Message = msg });
            return BadRequest(new { Message = msg });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  NOTIFICATIONS (BROADCAST)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Returns admin notifications.</summary>
        [HttpGet("notifications")]
        [SwaggerOperation(Summary = "Get admin notifications", Tags = new[] { "Notifications" })]
        public async Task<IActionResult> GetNotifications()
        {
            var n = await _notifications.GetByUserAsync(GetUserId());
            await _notifications.MarkAllReadAsync(GetUserId());
            return Ok(n);
        }

        /// <summary>
        /// Broadcasts a notification to all users of a given role (or all users).
        /// targetRole values: Citizen, Employer, LaborOfficer, ComplianceOfficer, GovernmentAuditor, ProgramManager, Admin, All
        /// </summary>
        [HttpPost("notifications/broadcast")]
        [SwaggerOperation(Summary = "Broadcast notification to a role", Tags = new[] { "Notifications" })]
        public async Task<IActionResult> BroadcastNotification(
            [FromQuery] string targetRole,
            [FromBody] string message)
        {
            var (success, msg) = await _admin.BroadcastNotificationAsync(targetRole, message, _notifications);
            if (success) await _logs.LogAsync(GetUserId(), "BroadcastNotification", targetRole);
            if (success) return Ok(new { Message = msg });
            return BadRequest(new { Message = msg });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  REPORTS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Returns all generated reports.</summary>
        [HttpGet("reports")]
        [SwaggerOperation(Summary = "Get all reports", Tags = new[] { "Reports" })]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _reports.GetAllAsync();
            return Ok(reports);
        }

        /// <summary>Generates a new report. reportType: Compliance, Financial, Program, User</summary>
        [HttpPost("reports")]
        [SwaggerOperation(Summary = "Generate a report", Tags = new[] { "Reports" })]
        public async Task<IActionResult> GenerateReport([FromQuery] string reportName, [FromQuery] string reportType)
        {
            var report = new Report
            {
                ReportName = reportName, ReportType = reportType,
                GeneratedBy = GetUserId(), GeneratedDate = DateTime.Now,
                ReportContent = $"Auto-generated {reportType} report at {DateTime.Now:yyyy-MM-dd HH:mm}"
            };
            var (success, msg) = await _reports.GenerateAsync(report);
            if (success) await _logs.LogAsync(GetUserId(), "GenerateReport", reportType);
            if (success) return Ok(new { Message = msg, Report = report });
            return BadRequest(new { Message = msg });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SYSTEM MONITORING
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Returns the most recent system activity logs.</summary>
        [HttpGet("system-logs")]
        [SwaggerOperation(Summary = "Get recent system logs", Tags = new[] { "System Monitoring" })]
        public async Task<IActionResult> GetSystemLogs([FromQuery] int count = 100)
        {
            var logs = await _logs.GetRecentAsync(count);
            return Ok(logs);
        }
    }
}
