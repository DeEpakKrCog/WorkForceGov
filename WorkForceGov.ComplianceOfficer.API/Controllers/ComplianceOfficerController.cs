using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Controllers
{
    [Route("api/compliance-officer")]
    [ApiController]
    [Produces("application/json")]
    public class ComplianceOfficerController : ControllerBase
    {
        private readonly IComplianceOfficerService _compliance;
        private readonly INotificationService _notifications;

        public ComplianceOfficerController(IComplianceOfficerService compliance, INotificationService notifications)
        { _compliance=compliance; _notifications=notifications; }

        private int GetUserId() {
            var c = User.FindFirst(ClaimTypes.NameIdentifier);
            if (c!=null&&int.TryParse(c.Value,out int j)) return j;
            if (Request.Headers.TryGetValue("X-User-Id",out var h)&&int.TryParse(h,out int p)) return p;
            throw new UnauthorizedAccessException("Provide X-User-Id header.");
        }

        [HttpGet("dashboard")]
        [SwaggerOperation(Summary="Get Compliance Officer dashboard",Tags=new[]{"Dashboard"})]
        public async Task<IActionResult> GetDashboard() => Ok(await _compliance.GetDashboardAsync(GetUserId()));

        [HttpGet("documents/pending")]
        [SwaggerOperation(Summary="Get pending employer documents",Tags=new[]{"Document Verification"})]
        public async Task<IActionResult> GetPending() => Ok(await _compliance.GetPendingEmployerDocumentsAsync());

        [HttpPut("documents/{id}/approve")]
        [SwaggerOperation(Summary="Approve an employer document",Tags=new[]{"Document Verification"})]
        public async Task<IActionResult> Approve(int id) {
            var (ok,msg) = await _compliance.ApproveEmployerDocumentAsync(id,GetUserId());
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpPut("documents/{id}/reject")]
        [SwaggerOperation(Summary="Reject an employer document (flags employer Suspended)",Tags=new[]{"Document Verification"})]
        public async Task<IActionResult> Reject(int id,[FromBody]string reason) {
            var (ok,msg) = await _compliance.RejectEmployerDocumentAsync(id,GetUserId(),reason);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpGet("employers")]
        [SwaggerOperation(Summary="Get all employers",Tags=new[]{"Employer Review"})]
        public async Task<IActionResult> GetEmployers() => Ok(await _compliance.GetAllEmployersAsync());

        [HttpGet("employers/by-status")]
        [SwaggerOperation(Summary="Filter employers by status (Pending/Verified/Flagged/Suspended)",Tags=new[]{"Employer Review"})]
        public async Task<IActionResult> GetByStatus([FromQuery]string status) => Ok(await _compliance.GetEmployersByStatusAsync(status));

        [HttpGet("employers/{id}")]
        [SwaggerOperation(Summary="Get employer details",Tags=new[]{"Employer Review"})]
        public async Task<IActionResult> GetEmployerDetails(int id) {
            var e = await _compliance.GetEmployerDetailsAsync(id);
            return e==null ? NotFound() : Ok(e);
        }

        [HttpPost("employers/{id}/flag")]
        [SwaggerOperation(Summary="Flag employer as non-compliant",Tags=new[]{"Non-Compliance"})]
        public async Task<IActionResult> FlagNonCompliant(int id,[FromBody]string reason) {
            var (ok,msg) = await _compliance.FlagEmployerNonCompliantAsync(id,GetUserId(),reason);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpPut("employers/{id}/clear-flag")]
        [SwaggerOperation(Summary="Clear non-compliance flag",Tags=new[]{"Non-Compliance"})]
        public async Task<IActionResult> ClearFlag(int id,[FromBody]string notes) {
            var (ok,msg) = await _compliance.ClearNonComplianceFlagAsync(id,GetUserId(),notes);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpGet("complaints")]
        [SwaggerOperation(Summary="Get all complaints",Tags=new[]{"Complaints"})]
        public async Task<IActionResult> GetComplaints() => Ok(await _compliance.GetAllComplaintsAsync());

        [HttpPut("complaints/{id}/resolve")]
        [SwaggerOperation(Summary="Resolve a complaint",Tags=new[]{"Complaints"})]
        public async Task<IActionResult> ResolveComplaint(int id,[FromBody]string resolution) {
            var (ok,msg) = await _compliance.ResolveComplaintAsync(id,GetUserId(),resolution);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpGet("compliance-records")]
        [SwaggerOperation(Summary="Get all compliance records",Tags=new[]{"Compliance Records"})]
        public async Task<IActionResult> GetRecords() => Ok(await _compliance.GetAllComplianceRecordsAsync());

        [HttpPost("compliance-records")]
        [SwaggerOperation(Summary="Create a compliance record",Tags=new[]{"Compliance Records"})]
        public async Task<IActionResult> CreateRecord([FromBody]ComplianceRecord record) {
            var (ok,msg) = await _compliance.CreateComplianceRecordAsync(record);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpPut("compliance-records/{id}/result")]
        [SwaggerOperation(Summary="Update compliance record result",Tags=new[]{"Compliance Records"})]
        public async Task<IActionResult> UpdateResult(int id,[FromQuery]string result,[FromBody]string notes) {
            var (ok,msg) = await _compliance.UpdateComplianceResultAsync(id,result,notes);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpGet("violations")]
        [SwaggerOperation(Summary="Get all violations",Tags=new[]{"Violations"})]
        public async Task<IActionResult> GetViolations() => Ok(await _compliance.GetAllViolationsAsync());

        [HttpPost("violations")]
        [SwaggerOperation(Summary="Record a violation",Tags=new[]{"Violations"})]
        public async Task<IActionResult> RecordViolation([FromBody]Violation violation) {
            var (ok,msg) = await _compliance.RecordViolationAsync(violation);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpGet("notifications")]
        [SwaggerOperation(Summary="Get notifications",Tags=new[]{"Notifications"})]
        public async Task<IActionResult> GetNotifications() {
            var uid=GetUserId(); var n=await _notifications.GetByUserAsync(uid);
            await _notifications.MarkAllReadAsync(uid); return Ok(n);
        }
    }
}
