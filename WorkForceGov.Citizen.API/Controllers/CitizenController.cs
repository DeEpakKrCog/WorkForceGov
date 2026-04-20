using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Controllers
{
    [Route("api/citizen")]
    [ApiController]
    [Authorize]                    // ← ADDED: requires valid JWT Bearer token
    [Produces("application/json")]
    public class CitizenController : ControllerBase
    {
        private readonly ICitizenService      _citizen;
        private readonly IJobService          _jobs;
        private readonly IApplicationService  _apps;
        private readonly IDocumentService     _docs;
        private readonly IBenefitService      _benefits;
        private readonly INotificationService _notifications;
        private readonly IProgramService      _programs;
        private readonly ITrainingService     _trainings;

        public CitizenController(ICitizenService citizen, IJobService jobs, IApplicationService apps,
            IDocumentService docs, IBenefitService benefits, INotificationService notifications,
            IProgramService programs, ITrainingService trainings)
        {
            _citizen       = citizen;  _jobs    = jobs;    _apps  = apps;
            _docs          = docs;     _benefits = benefits;
            _notifications = notifications;
            _programs      = programs; _trainings = trainings;
        }

        // Reads UserId from the JWT "sub" claim (set by AuthController on login)
        private int GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier);
            if (c != null && int.TryParse(c.Value, out int id)) return id;
            throw new UnauthorizedAccessException("Valid JWT token required.");
        }

        [HttpGet("dashboard")]
        [SwaggerOperation(Summary = "Get citizen dashboard", Tags = new[] { "Dashboard" })]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = GetUserId();
            if (await _citizen.GetByUserIdAsync(userId) == null)
                await _citizen.CreateProfileAsync(userId, "New User", "");
            return Ok(await _citizen.GetDashboardAsync(userId));
        }

        [HttpGet("profile")]
        [SwaggerOperation(Summary = "Get citizen profile", Tags = new[] { "Profile" })]
        public async Task<IActionResult> GetProfile()
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            return c == null ? NotFound(new { Message = "Profile not found." }) : Ok(c);
        }

        [HttpPut("profile")]
        [SwaggerOperation(Summary = "Update citizen profile", Tags = new[] { "Profile" })]
        public async Task<IActionResult> UpdateProfile([FromBody] Citizen model)
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });
            c.FullName = model.FullName; c.DOB = model.DOB; c.Gender = model.Gender;
            c.Address = model.Address;   c.PhoneNumber = model.PhoneNumber;
            var (ok, msg) = await _citizen.UpdateProfileAsync(c);
            return ok ? Ok(new { Message = "Profile updated.", Citizen = c }) : BadRequest(new { Message = msg });
        }

        [HttpGet("jobs/search")]
        [SwaggerOperation(Summary = "Search job openings", Tags = new[] { "Jobs" })]
        public async Task<IActionResult> SearchJobs(
            [FromQuery] string? keyword, [FromQuery] string? location, [FromQuery] string? category)
            => Ok(new { Keyword = keyword, Location = location, Category = category,
                        Jobs = await _jobs.SearchAsync(keyword, location, category) });

        [HttpPost("jobs/{jobId}/apply")]
        [SwaggerOperation(Summary = "Apply for a job", Tags = new[] { "Jobs" })]
        public async Task<IActionResult> ApplyForJob(int jobId, [FromBody] string? coverLetter)
        {
            var userId = GetUserId();
            var c = await _citizen.GetByUserIdAsync(userId);
            if (c == null) return NotFound(new { Message = "Profile required." });
            var docs = await _citizen.GetDocumentsAsync(c.Id);
            if (!docs.Any(d => d.DocumentType.ToLower().Contains("resume")))
                return BadRequest(new { Message = "Upload a Resume before applying." });
            var (ok, msg) = await _citizen.ApplyForJobAsync(c.Id, jobId, coverLetter);
            return ok ? Ok(new { Message = "Application submitted!" }) : BadRequest(new { Message = msg });
        }

        [HttpGet("applications")]
        [SwaggerOperation(Summary = "Get my job applications", Tags = new[] { "Applications" })]
        public async Task<IActionResult> GetApplications()
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });
            return Ok(await _apps.GetByCitizenAsync(c.Id));
        }

        [HttpDelete("applications/{id}")]
        [SwaggerOperation(Summary = "Withdraw a job application", Tags = new[] { "Applications" })]
        public async Task<IActionResult> WithdrawApplication(int id)
        {
            var (ok, msg) = await _apps.WithdrawAsync(id);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpGet("documents")]
        [SwaggerOperation(Summary = "Get my documents", Tags = new[] { "Documents" })]
        public async Task<IActionResult> GetDocuments()
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });
            return Ok(await _docs.GetByCitizenAsync(c.Id));
        }

        [HttpGet("benefits")]
        [SwaggerOperation(Summary = "Get my benefits", Tags = new[] { "Benefits" })]
        public async Task<IActionResult> GetBenefits()
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });
            return Ok(await _benefits.GetByCitizenAsync(c.Id));
        }

        [HttpGet("trainings")]
        [SwaggerOperation(Summary = "Get available trainings + my enrollments", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> GetTrainings()
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });
            var all      = await _trainings.GetAllTrainingsAsync();
            var enrolled = await _trainings.GetEnrollmentsByCitizenAsync(c.Id);
            return Ok(new { Available = all.Where(t => t.Status == "Active"), Enrollments = enrolled });
        }

        [HttpGet("notifications")]
        [SwaggerOperation(Summary = "Get my notifications", Tags = new[] { "Notifications" })]
        public async Task<IActionResult> GetNotifications()
        {
            var notifs = await _notifications.GetByUserAsync(GetUserId());
            await _notifications.MarkAllReadAsync(GetUserId());
            return Ok(notifs);
        }
    }
}
