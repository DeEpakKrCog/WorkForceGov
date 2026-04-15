using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Interfaces.Services;

namespace WorkForceGovProject.Controllers
{
    [Route("api/auditor")]
    [ApiController]
    [Produces("application/json")]
    public class GovernmentAuditorController : ControllerBase
    {
        private readonly IGovernmentAuditorService _auditor;
        private readonly IAuditService _audit;
        private readonly IReportService _report;
        private readonly INotificationService _notifications;

        public GovernmentAuditorController(IGovernmentAuditorService auditor,IAuditService audit,IReportService report,INotificationService notifications)
        { _auditor=auditor;_audit=audit;_report=report;_notifications=notifications; }

        private int GetUserId() {
            var c=User.FindFirst(ClaimTypes.NameIdentifier);
            if(c!=null&&int.TryParse(c.Value,out int j)) return j;
            if(Request.Headers.TryGetValue("X-User-Id",out var h)&&int.TryParse(h,out int p)) return p;
            throw new UnauthorizedAccessException("Provide X-User-Id header.");
        }

        [HttpGet("dashboard")]
        [SwaggerOperation(Summary="Get Auditor dashboard",Tags=new[]{"Dashboard"})]
        public async Task<IActionResult> GetDashboard() => Ok(await _auditor.GetDashboardAsync(GetUserId()));

        [HttpGet("audits")]
        [SwaggerOperation(Summary="Get all audits",Tags=new[]{"Audits"})]
        public async Task<IActionResult> GetAudits() => Ok(await _auditor.GetAllAuditsAsync());

        [HttpGet("audits/{id}")]
        [SwaggerOperation(Summary="Get audit by ID",Tags=new[]{"Audits"})]
        public async Task<IActionResult> GetAudit(int id) {
            var a=await _auditor.GetAuditByIdAsync(id);
            return a==null?NotFound():Ok(a);
        }

        [HttpPost("audits")]
        [SwaggerOperation(Summary="Create a new audit",Tags=new[]{"Audits"})]
        public async Task<IActionResult> CreateAudit([FromQuery]string scope,[FromBody]string findings) {
            var (ok,msg,audit)=await _auditor.CreateAuditAsync(GetUserId(),scope,findings);
            return ok?Ok(new{Message=msg,Audit=audit}):BadRequest(new{Message=msg});
        }

        [HttpPut("audits/{id}/status")]
        [SwaggerOperation(Summary="Update audit status (Open/In Progress/Completed)",Tags=new[]{"Audits"})]
        public async Task<IActionResult> UpdateStatus(int id,[FromQuery]string newStatus) {
            var (ok,msg)=await _auditor.UpdateAuditStatusAsync(id,newStatus);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        [HttpPut("audits/{id}/findings")]
        [SwaggerOperation(Summary="Record or update audit findings",Tags=new[]{"Audits"})]
        public async Task<IActionResult> RecordFindings(int id,[FromBody]string findings) {
            var (ok,msg)=await _auditor.RecordAuditFindingsAsync(id,findings);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        [HttpGet("compliance")]
        [SwaggerOperation(Summary="Get all compliance records",Tags=new[]{"Compliance Review"})]
        public async Task<IActionResult> GetCompliance() => Ok(await _auditor.GetComplianceRecordsAsync());

        [HttpPost("compliance/escalate")]
        [SwaggerOperation(Summary="Escalate a compliance issue (entityType: Employer or Program)",Tags=new[]{"Compliance Review"})]
        public async Task<IActionResult> Escalate([FromQuery]int entityId,[FromQuery]string entityType,[FromBody]string notes) {
            var (ok,msg)=await _auditor.EscalateComplianceIssueAsync(entityId,entityType,GetUserId(),notes);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        [HttpGet("programs")]
        [SwaggerOperation(Summary="Get all programs for review",Tags=new[]{"Program Review"})]
        public async Task<IActionResult> GetPrograms() => Ok(await _auditor.GetAllProgramsAsync());

        [HttpPost("programs/{id}/flag")]
        [SwaggerOperation(Summary="Flag a program for investigation",Tags=new[]{"Program Review"})]
        public async Task<IActionResult> FlagProgram(int id,[FromBody]string reason) {
            var (ok,msg)=await _auditor.FlagProgramForReviewAsync(id,GetUserId(),reason);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        [HttpGet("employers")]
        [SwaggerOperation(Summary="Get all employers (audit view)",Tags=new[]{"Employer Review"})]
        public async Task<IActionResult> GetEmployers() => Ok(await _auditor.GetAllEmployersAsync());

        [HttpGet("reports")]
        [SwaggerOperation(Summary="Get all audit reports",Tags=new[]{"Reports"})]
        public async Task<IActionResult> GetReports() => Ok(await _auditor.GetAuditReportsAsync());

        [HttpPost("reports")]
        [SwaggerOperation(Summary="Generate an audit report (Compliance/Financial/Program/Employer)",Tags=new[]{"Reports"})]
        public async Task<IActionResult> GenerateReport([FromQuery]string reportName,[FromQuery]string reportType,[FromBody]string content) {
            var (ok,msg,report)=await _auditor.GenerateAuditReportAsync(GetUserId(),reportName,reportType,content);
            return ok?Ok(new{Message=msg,Report=report}):BadRequest(new{Message=msg});
        }

        [HttpGet("notifications")]
        [SwaggerOperation(Summary="Get notifications",Tags=new[]{"Notifications"})]
        public async Task<IActionResult> GetNotifications() {
            var uid=GetUserId(); var n=await _notifications.GetByUserAsync(uid);
            await _notifications.MarkAllReadAsync(uid); return Ok(n);
        }
    }
}
