using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Data; // 🚨 Added to access the database directly

namespace WorkForceGovProject.Controllers
{
    [Route("api/labor-officer")]
    [ApiController]
    [Produces("application/json")]
    public class LaborOfficerController : ControllerBase
    {
        private readonly ILaborOfficerService _labor;
        private readonly IComplianceService _compliance;
        private readonly IComplianceOfficerService _complianceOfficer;
        private readonly INotificationService _notifications;
        private readonly ApplicationDbContext _context; // 🚨 Added database context

        public LaborOfficerController(ILaborOfficerService labor, IComplianceService compliance,
            IComplianceOfficerService complianceOfficer, INotificationService notifications,
            ApplicationDbContext context) // 🚨 Injected here
        {
            _labor = labor;
            _compliance = compliance;
            _complianceOfficer = complianceOfficer;
            _notifications = notifications;
            _context = context;
        }

        private int? TryGetUserId()
        {
            var c = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
            if (c != null && int.TryParse(c.Value, out int j)) return j;
            if (Request?.Headers?.TryGetValue("X-User-Id", out var h) == true && int.TryParse(h, out int p)) return p;
            return null;
        }

        [HttpGet("dashboard")]
        [SwaggerOperation(Summary = "Get Labor Officer dashboard", Tags = new[] { "Dashboard" })]
        public async Task<IActionResult> GetDashboard()
        {
            var uid = TryGetUserId();
            if (uid == null) return Unauthorized(new { Message = "Provide X-User-Id header or authenticate." });

            // 1. Get the base dashboard data
            var dashboard = await _labor.GetDashboardAsync(uid.Value);

            // 2. Fetch all employer documents from the database
            var employerDocs = await _context.EmployerDocuments.ToListAsync();

            // 3. 🚨 FIX: Add Employer counts to the existing Citizen counts!
            dashboard.PendingDocuments += employerDocs.Count(d => d.VerificationStatus == "Pending");
            dashboard.ApprovedApplications += employerDocs.Count(d => d.VerificationStatus == "Verified" || d.VerificationStatus == "Approved");
            dashboard.RejectedApplications += employerDocs.Count(d => d.VerificationStatus == "Rejected");

            return Ok(dashboard);
        }

        // ── CITIZEN DOCUMENT VERIFICATION ──────────────────────────────────
        [HttpGet("documents/citizen/pending")]
        [SwaggerOperation(Summary = "Get pending citizen documents", Tags = new[] { "Document Verification" })]
        public async Task<IActionResult> GetPendingCitizenDocs() => Ok(await _labor.GetPendingCitizenDocumentsAsync());

        [HttpPut("documents/citizen/{id}/verify")]
        [SwaggerOperation(Summary = "Verify a citizen document", Tags = new[] { "Document Verification" })]
        public async Task<IActionResult> VerifyDocument(int id)
        {
            var uid = TryGetUserId();
            if (uid == null) return Unauthorized(new { Message = "Provide X-User-Id header or authenticate." });
            var (ok, msg) = await _labor.VerifyCitizenDocumentAsync(id, uid.Value);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("documents/citizen/{id}/reject")]
        [SwaggerOperation(Summary = "Reject a citizen document", Tags = new[] { "Document Verification" })]
        public async Task<IActionResult> RejectDocument(int id, [FromBody] string reason)
        {
            var uid = TryGetUserId();
            if (uid == null) return Unauthorized(new { Message = "Provide X-User-Id header or authenticate." });
            var (ok, msg) = await _labor.RejectCitizenDocumentAsync(id, uid.Value, reason);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // ── EMPLOYER DOCUMENT VERIFICATION ─────────────────────────────────
        [HttpGet("documents/employer/pending")]
        [SwaggerOperation(Summary = "Get pending employer documents", Tags = new[] { "Document Verification" })]
        public async Task<IActionResult> GetPendingEmployerDocs() => Ok(await _complianceOfficer.GetPendingEmployerDocumentsAsync());

        [HttpPut("documents/employer/{id}/verify")]
        [SwaggerOperation(Summary = "Verify an employer document", Tags = new[] { "Document Verification" })]
        public async Task<IActionResult> VerifyEmployerDocument(int id)
        {
            var uid = TryGetUserId();
            if (uid == null) return Unauthorized(new { Message = "Provide X-User-Id header or authenticate." });
            var (ok, msg) = await _complianceOfficer.VerifyEmployerDocumentAsync(id, uid.Value);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("documents/employer/{id}/reject")]
        [SwaggerOperation(Summary = "Reject an employer document", Tags = new[] { "Document Verification" })]
        public async Task<IActionResult> RejectEmployerDocument(int id, [FromBody] string reason)
        {
            var uid = TryGetUserId();
            if (uid == null) return Unauthorized(new { Message = "Provide X-User-Id header or authenticate." });
            var (ok, msg) = await _complianceOfficer.RejectEmployerDocumentAsync(id, uid.Value, reason);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // ── COMPLAINTS ──────────────────────────────────────────────────────
        [HttpGet("complaints")]
        [SwaggerOperation(Summary = "Get all complaints", Tags = new[] { "Complaints" })]
        public async Task<IActionResult> GetComplaints() => Ok(await _labor.GetAllComplaintsAsync());

        [HttpGet("complaints/pending")]
        [SwaggerOperation(Summary = "Get pending complaints", Tags = new[] { "Complaints" })]
        public async Task<IActionResult> GetPendingComplaints() => Ok(await _labor.GetPendingComplaintsAsync());

        [HttpPut("complaints/{id}/investigate")]
        [SwaggerOperation(Summary = "Investigate a complaint", Tags = new[] { "Complaints" })]
        public async Task<IActionResult> InvestigateComplaint(int id, [FromBody] string resolution, [FromQuery] string newStatus = "Resolved")
        {
            var (ok, msg) = await _labor.InvestigateComplaintAsync(id, resolution, newStatus);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // ── COMPLIANCE RECORDS ──────────────────────────────────────────────
        [HttpGet("compliance")]
        [SwaggerOperation(Summary = "Get all compliance records", Tags = new[] { "Compliance" })]
        public async Task<IActionResult> GetComplianceRecords() => Ok(await _labor.GetComplianceRecordsAsync());

        [HttpPost("compliance")]
        [SwaggerOperation(Summary = "Create a compliance record", Tags = new[] { "Compliance" })]
        public async Task<IActionResult> CreateComplianceRecord([FromBody] ComplianceRecord record)
        {
            var (ok, msg) = await _labor.CreateComplianceRecordAsync(record);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // ── VIOLATIONS ──────────────────────────────────────────────────────
        [HttpGet("violations")]
        [SwaggerOperation(Summary = "Get all violations", Tags = new[] { "Violations" })]
        public async Task<IActionResult> GetViolations() => Ok(await _labor.GetViolationsAsync());

        [HttpPost("violations")]
        [SwaggerOperation(Summary = "Record a new violation", Tags = new[] { "Violations" })]
        public async Task<IActionResult> RecordViolation([FromBody] Violation violation)
        {
            var (ok, msg) = await _labor.RecordViolationAsync(violation);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // ── AUDITS ──────────────────────────────────────────────────────────
        // 🚨 APPLIED FIX: Added the GetAudits endpoint that your Angular frontend is looking for
        [HttpGet("audits")]
        [SwaggerOperation(Summary = "Get all audits", Tags = new[] { "Audits" })]
        public async Task<IActionResult> GetAudits()
        {
            var audits = await _context.Audits.ToListAsync();
            return Ok(audits);
        }

        // ── APPLICATIONS OVERSIGHT ──────────────────────────────────────────
        [HttpGet("applications")]
        [SwaggerOperation(Summary = "Get all applications (oversight)", Tags = new[] { "Applications" })]
        public async Task<IActionResult> GetApplications() => Ok(await _labor.GetAllApplicationsAsync());

        [HttpPut("applications/{id}/approve")]
        [SwaggerOperation(Summary = "Approve a job application", Tags = new[] { "Applications" })]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            var (ok, msg) = await _labor.ApproveApplicationAsync(id);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("applications/{id}/reject")]
        [SwaggerOperation(Summary = "Reject a job application", Tags = new[] { "Applications" })]
        public async Task<IActionResult> RejectApplication(int id, [FromBody] string notes)
        {
            var (ok, msg) = await _labor.RejectApplicationAsync(id, notes);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("applications/{id}/flag")]
        [SwaggerOperation(Summary = "Flag a suspicious application", Tags = new[] { "Applications" })]
        public async Task<IActionResult> FlagApplication(int id, [FromBody] string notes)
        {
            var (ok, msg) = await _labor.FlagApplicationAsync(id, notes);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpGet("notifications")]
        [SwaggerOperation(Summary = "Get notifications", Tags = new[] { "Notifications" })]
        public async Task<IActionResult> GetNotifications()
        {
            var uid = TryGetUserId();
            if (uid == null) return Unauthorized(new { Message = "Provide X-User-Id header or authenticate." });
            var n = await _notifications.GetByUserAsync(uid.Value);
            await _notifications.MarkAllReadAsync(uid.Value);
            return Ok(n);
        }
    }
}