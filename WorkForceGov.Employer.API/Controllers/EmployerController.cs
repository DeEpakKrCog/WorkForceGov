using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Controllers
{
    [Route("api/employer")]
    [ApiController]
    [Authorize]                    // ← ADDED: requires valid JWT Bearer token
    [Produces("application/json")]
    public class EmployerController : ControllerBase
    {
        private readonly IEmployerService _employer;
        private readonly IJobService _jobs;
        private readonly INotificationService _notifications;

        public EmployerController(IEmployerService employer, IJobService jobs, INotificationService notifications)
        { _employer = employer; _jobs = jobs; _notifications = notifications; }

        // Reads UserId from the JWT "sub" claim (set by AuthController on login)
        private int GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier);
            if (c != null && int.TryParse(c.Value, out int id)) return id;
            throw new UnauthorizedAccessException("Valid JWT token required.");
        }

        [HttpGet("dashboard")]
        [SwaggerOperation(Summary = "Get employer dashboard", Tags = new[] { "Dashboard" })]
        public async Task<IActionResult> GetDashboard()
            => Ok(await _employer.GetDashboardAsync(GetUserId()));

        [HttpGet("profile")]
        [SwaggerOperation(Summary = "Get employer profile", Tags = new[] { "Profile" })]
        public async Task<IActionResult> GetProfile()
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            return e == null ? NotFound(new { Message = "Profile not found." }) : Ok(e);
        }

        [HttpPost("profile/register")]
        [SwaggerOperation(Summary = "Register employer profile", Tags = new[] { "Profile" })]
        public async Task<IActionResult> Register([FromBody] Employer model)
        {
            var (ok, msg) = await _employer.RegisterAsync(GetUserId(), model);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("profile")]
        [SwaggerOperation(Summary = "Update employer profile", Tags = new[] { "Profile" })]
        public async Task<IActionResult> UpdateProfile([FromBody] Employer model)
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            if (e == null) return NotFound(new { Message = "Profile not found." });
            e.CompanyName = model.CompanyName; e.Industry = model.Industry;
            e.Address = model.Address; e.PhoneNumber = model.PhoneNumber;
            e.Website = model.Website; e.Description = model.Description;
            var (ok, msg) = await _employer.UpdateProfileAsync(e);
            return ok ? Ok(new { Message = "Profile updated.", Employer = e }) : BadRequest(new { Message = msg });
        }

        [HttpGet("documents")]
        [SwaggerOperation(Summary = "Get employer documents", Tags = new[] { "Documents" })]
        public async Task<IActionResult> GetDocuments()
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            if (e == null) return NotFound(new { Message = "Profile not found." });
            return Ok(await _employer.GetDocumentsAsync(e.Id));
        }

        [HttpPost("documents")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Upload employer document", Tags = new[] { "Documents" })]
        public async Task<IActionResult> UploadDocument([FromForm] string documentType, IFormFile file)
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            if (e == null) return NotFound(new { Message = "Profile not found." });
            if (file == null || file.Length == 0) return BadRequest(new { Message = "Select a valid file." });
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "employer-docs");
            Directory.CreateDirectory(folder);
            var fn = $"{e.Id}_{documentType}_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
            using (var s = new FileStream(Path.Combine(folder, fn), FileMode.Create)) await file.CopyToAsync(s);
            var url = $"/uploads/employer-docs/{fn}";
            var (ok, msg, doc) = await _employer.UploadDocumentAsync(e.Id, documentType, url);
            return ok ? Ok(new { Message = msg, Document = doc, FileUrl = url }) : BadRequest(new { Message = msg });
        }

        [HttpGet("jobs")]
        [SwaggerOperation(Summary = "Get my job postings", Tags = new[] { "Jobs" })]
        public async Task<IActionResult> GetJobs()
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            return e == null ? NotFound() : Ok(await _employer.GetJobsAsync(e.Id));
        }

        [HttpPost("jobs")]
        [SwaggerOperation(Summary = "Post a new job opening", Tags = new[] { "Jobs" })]
        public async Task<IActionResult> PostJob([FromBody] JobOpening job)
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            if (e == null) return NotFound();
            job.EmployerId = e.Id; job.PostedDate = DateTime.Now; job.Status = "Open";
            var (ok, msg) = await _employer.PostJobAsync(job);
            return ok ? Ok(new { Message = "Job posted.", Job = job }) : BadRequest(new { Message = msg });
        }

        [HttpPut("jobs/{jobId}")]
        [SwaggerOperation(Summary = "Update a job posting", Tags = new[] { "Jobs" })]
        public async Task<IActionResult> UpdateJob(int jobId, [FromBody] JobOpening job)
        {
            job.Id = jobId;
            var (ok, msg) = await _employer.UpdateJobAsync(job);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("jobs/{jobId}/close")]
        [SwaggerOperation(Summary = "Close a job posting", Tags = new[] { "Jobs" })]
        public async Task<IActionResult> CloseJob(int jobId)
        {
            var (ok, msg) = await _employer.CloseJobAsync(jobId);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpGet("applications")]
        [SwaggerOperation(Summary = "Get received applications", Tags = new[] { "Applications" })]
        public async Task<IActionResult> GetApplications()
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            return e == null ? NotFound() : Ok(await _employer.GetApplicationsReceivedAsync(e.Id));
        }

        [HttpGet("applications/{appId}")]
        [SwaggerOperation(Summary = "Get application details", Tags = new[] { "Applications" })]
        public async Task<IActionResult> GetApplicationDetails(int appId)
        {
            var app = await _employer.GetApplicationDetailsAsync(appId);
            return app == null ? NotFound() : Ok(app);
        }

        [HttpPut("applications/{appId}/status")]
        [SwaggerOperation(Summary = "Update application status", Tags = new[] { "Applications" })]
        public async Task<IActionResult> UpdateApplicationStatus(int appId, [FromQuery] string status, [FromBody] string? notes)
        {
            var (ok, msg) = await _employer.UpdateApplicationStatusAsync(appId, status, notes);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpGet("notifications")]
        [SwaggerOperation(Summary = "Get notifications", Tags = new[] { "Notifications" })]
        public async Task<IActionResult> GetNotifications()
        {
            var uid = GetUserId();
            var n = await _notifications.GetByUserAsync(uid);
            await _notifications.MarkAllReadAsync(uid);
            return Ok(n);
        }
    }
}
