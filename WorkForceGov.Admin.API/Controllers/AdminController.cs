using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
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

        [HttpGet("dashboard")]
        [SwaggerOperation(Summary = "Get admin dashboard", Tags = new[] { "Dashboard" })]
        public async Task<IActionResult> GetDashboard()
        {
            var model = await _admin.GetFullDashboardAsync();
            return Ok(model);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  USER MANAGEMENT
        // ═══════════════════════════════════════════════════════════════════

        [HttpGet("users")]
        [SwaggerOperation(Summary = "Get all users", Tags = new[] { "User Management" })]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _account.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("users/{id}")]
        [SwaggerOperation(Summary = "Get user by ID", Tags = new[] { "User Management" })]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _account.GetByIdAsync(id);
            if (user == null) return NotFound(new { Message = "User not found." });
            return Ok(user);
        }

        [HttpPost("users")]
        [SwaggerOperation(Summary = "Create a user", Tags = new[] { "User Management" })]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserViewModel model)
        {
            var (success, msg) = await _account.CreateUserAsync(model);
            if (success)
            {
                await _logs.LogAsync(GetUserId(), "CreateUser", $"Email: {model.Email}");
                return Ok(new { Message = msg });
            }
            return BadRequest(new { Message = msg });
        }

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
            if (success)
            {
                await _logs.LogAsync(GetUserId(), "UpdateUser", $"UserId: {id}");
                return Ok(new { Message = "User updated.", User = existing });
            }
            return BadRequest(new { Message = msg });
        }

        [HttpPut("users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var user = await _account.GetByIdAsync(id);
            if (user == null) return NotFound(new { Message = "User not found." });

            user.Status = "Inactive"; // Change status instead of deleting
            var (success, msg) = await _account.UpdateUserAsync(user);

            if (success)
            {
                await _logs.LogAsync(GetUserId(), "DeactivateUser", $"UserId: {id}");
                return Ok(new { Message = "User deactivated successfully." });
            }
            return BadRequest(new { Message = msg });
        }

        [HttpPut("users/{id}/activate")]
        [SwaggerOperation(Summary = "Activate a deactivated user", Tags = new[] { "User Management" })]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var user = await _account.GetByIdAsync(id);
            if (user == null) return NotFound(new { Message = "User not found." });

            user.Status = "Active";
            var (success, msg) = await _account.UpdateUserAsync(user);
            if (success)
            {
                await _logs.LogAsync(GetUserId(), "ActivateUser", $"UserId: {id}");
                return Ok(new { Message = $"User '{user.FullName}' activated." });
            }
            return BadRequest(new { Message = msg });
        }

        [HttpDelete("users/{id}")]
        [SwaggerOperation(Summary = "Permanently delete a user", Tags = new[] { "User Management" })]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var (success, msg) = await _account.DeleteUserAsync(id);
            if (success)
            {
                await _logs.LogAsync(GetUserId(), "DeleteUser", $"UserId: {id}");
                return Ok(new { Message = msg });
            }
            return BadRequest(new { Message = msg });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  EMPLOYER OVERSIGHT
        // ═══════════════════════════════════════════════════════════════════

        [HttpGet("employers")]
        [SwaggerOperation(Summary = "Get all employers", Tags = new[] { "Employer Oversight" })]
        public async Task<IActionResult> GetEmployers([FromQuery] string? status)
        {
            var employers = await _admin.GetAllEmployersAsync(status);
            return Ok(employers);
        }

        [HttpPut("employers/{id}/suspend")]
        [SwaggerOperation(Summary = "Suspend an employer", Tags = new[] { "Employer Oversight" })]
        public async Task<IActionResult> SuspendEmployer(int id, [FromBody] string reason)
        {
            var (success, msg) = await _admin.SuspendEmployerAsync(id, reason);
            if (success) await _logs.LogAsync(GetUserId(), "SuspendEmployer", "EmployerId=" + id);
            if (success) return Ok(new { Message = msg });
            return BadRequest(new { Message = msg });
        }

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
        //  NOTIFICATIONS
        // ═══════════════════════════════════════════════════════════════════

        [HttpGet("notifications")]
        [SwaggerOperation(Summary = "Get admin notifications", Tags = new[] { "Notifications" })]
        public async Task<IActionResult> GetNotifications()
        {
            var n = await _notifications.GetByUserAsync(GetUserId());
            await _notifications.MarkAllReadAsync(GetUserId());
            return Ok(n);
        }

        [HttpPost("notifications/broadcast")]
        [SwaggerOperation(Summary = "Broadcast notification to a role", Tags = new[] { "Notifications" })]
        public async Task<IActionResult> BroadcastNotification([FromQuery] string targetRole, [FromBody] string message)
        {
            var (success, msg) = await _admin.BroadcastNotificationAsync(targetRole, message, _notifications);
            if (success) await _logs.LogAsync(GetUserId(), "BroadcastNotification", targetRole);
            if (success) return Ok(new { Message = msg });
            return BadRequest(new { Message = msg });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  REPORTS
        // ═══════════════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════════════
        //  REPORTS
        // ═══════════════════════════════════════════════════════════════════

        [HttpGet("reports")]
        [SwaggerOperation(Summary = "Get all reports", Tags = new[] { "Reports" })]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _reports.GetAllAsync();
            return Ok(reports.OrderByDescending(r => r.GeneratedDate));
        }

        // Added StartDate and EndDate
        public record GenerateReportRequest(string ReportName, string ReportType, DateTime? StartDate, DateTime? EndDate);

        [HttpPost("reports")]
        [SwaggerOperation(Summary = "Generate a report with real data", Tags = new[] { "Reports" })]
        public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequest request)
        {
            var csvData = new System.Text.StringBuilder();

            // ── GENERATE ACTUAL DATA BASED ON TYPE ──
            switch (request.ReportType)
            {
                case "UserActivity":
                    // Pull real system logs
                    var logs = await _logs.GetRecentAsync(5000); // Get up to 5000 entries

                    // Optional Date Filtering
                    if (request.StartDate.HasValue) logs = logs.Where(l => l.Timestamp >= request.StartDate.Value);
                    if (request.EndDate.HasValue) logs = logs.Where(l => l.Timestamp <= request.EndDate.Value.AddDays(1));

                    csvData.AppendLine("LogID,Action,Resource,Timestamp");
                    foreach (var log in logs)
                    {
                        // Replace commas to prevent breaking the CSV format
                        var safeResource = log.Resource?.Replace(",", " ") ?? "N/A";
                        csvData.AppendLine($"{log.Id},{log.Action},{safeResource},{log.Timestamp:yyyy-MM-dd HH:mm:ss}");
                    }
                    break;

                case "Compliance":
                    // Pull real user accounts
                    var users = await _account.GetAllUsersAsync();

                    csvData.AppendLine("UserID,FullName,Email,Role,Status,Phone");
                    foreach (var u in users)
                    {
                        var safeName = u.FullName?.Replace(",", " ");
                        csvData.AppendLine($"{u.Id},{safeName},{u.Email},{u.Role},{u.Status},{u.Phone}");
                    }
                    break;

                case "Employment":
                    // Pull real employer data
                    var employers = await _admin.GetAllEmployersAsync(null);

                    csvData.AppendLine("EmployerID,CompanyName,Industry,Status,VerificationDate");
                    foreach (var emp in employers)
                    {
                        // Assuming Employer model has these, adjust properties as needed based on your actual model
                        var safeCompany = emp.CompanyName?.Replace(",", " ");
                        csvData.AppendLine($"{emp.Id},{safeCompany},{emp.Industry},{emp.Status},N/A");
                    }
                    break;

                default:
                    csvData.AppendLine("ReportId,DateGenerated,Type,Status");
                    csvData.AppendLine($"{Guid.NewGuid().ToString().Substring(0, 8)},{DateTime.UtcNow:yyyy-MM-dd},{request.ReportType},NO_DATA_MAPPING");
                    break;
            }

            // Save the report metadata and content to the database
            var report = new Report
            {
                ReportName = request.ReportName,
                ReportType = request.ReportType,
                GeneratedBy = GetUserId(),
                GeneratedDate = DateTime.Now,
                ReportContent = csvData.ToString()
            };

            var (success, msg) = await _reports.GenerateAsync(report);
            if (success) await _logs.LogAsync(GetUserId(), "GenerateReport", request.ReportType);
            if (success) return Ok(new { Message = "Report generated successfully.", Report = report });
            return BadRequest(new { Message = msg });
        }

        [HttpGet("reports/{id}/download")]
        [SwaggerOperation(Summary = "Download a report as CSV", Tags = new[] { "Reports" })]
        public async Task<IActionResult> DownloadReport(int id)
        {
            var reports = await _reports.GetAllAsync();
            var report = reports.FirstOrDefault(r => r.Id == id);

            if (report == null) return NotFound("Report not found.");

            // Add BOM (Byte Order Mark) so Excel reads special characters correctly
            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(report.ReportContent ?? "No data");
            var fileBytes = preamble.Concat(contentBytes).ToArray();

            var fileName = $"{report.ReportName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv";

            return File(fileBytes, "text/csv", fileName);
        }
        // ═══════════════════════════════════════════════════════════════════
        //  SYSTEM MONITORING
        // ═══════════════════════════════════════════════════════════════════

        [HttpGet("system-logs")]
        [SwaggerOperation(Summary = "Get recent system logs", Tags = new[] { "System Monitoring" })]
        public async Task<IActionResult> GetSystemLogs([FromQuery] int count = 100)
        {
            var logs = await _logs.GetRecentAsync(count);
            return Ok(logs);
        }
    }
}