using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Controllers
{
    [Route("api/citizen")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class CitizenController : ControllerBase
    {
        private readonly ICitizenService _citizen;
        private readonly IJobService _jobs;
        private readonly IApplicationService _apps;
        private readonly IDocumentService _docs;
        private readonly IBenefitService _benefits;
        private readonly INotificationService _notifications;
        private readonly IProgramService _programs;
        private readonly ITrainingService _trainings;
        private readonly IAccountService _account; // Corrected to use AccountService

        public CitizenController(
            ICitizenService citizen, IJobService jobs, IApplicationService apps,
            IDocumentService docs, IBenefitService benefits, INotificationService notifications,
            IProgramService programs, ITrainingService trainings,
            IAccountService account)
        {
            _citizen = citizen; _jobs = jobs; _apps = apps;
            _docs = docs; _benefits = benefits;
            _notifications = notifications;
            _programs = programs; _trainings = trainings;
            _account = account;
        }

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
            var citizen = await _citizen.GetByUserIdAsync(userId);

            // SYNC FIX: Fetch real name from Users table instead of hardcoding "New User"
            if (citizen == null)
            {
                var user = await _account.GetByIdAsync(userId);
                if (user != null)
                {
                    await _citizen.CreateProfileAsync(userId, user.FullName, user.Email);
                }
                else
                {
                    await _citizen.CreateProfileAsync(userId, "New User", "");
                }
            }

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
            var userId = GetUserId();
            var existing = await _citizen.GetByUserIdAsync(userId);
            if (existing == null) return NotFound(new { Message = "Profile not found." });

            // 400 ERROR FIX: Manually map fields to the tracked entity
            existing.FullName = model.FullName;
            existing.DOB = model.DOB;
            existing.Gender = model.Gender;
            existing.Address = model.Address;
            existing.PhoneNumber = model.PhoneNumber;

            var (ok, msg) = await _citizen.UpdateProfileAsync(existing);
            return ok ? Ok(new { Message = "Profile updated.", Citizen = existing }) : BadRequest(new { Message = msg });
        }

        [HttpGet("jobs/search")]
        [SwaggerOperation(Summary = "Search job openings", Tags = new[] { "Jobs" })]
        public async Task<IActionResult> SearchJobs(
            [FromQuery] string? keyword, [FromQuery] string? location, [FromQuery] string? category)
            => Ok(new
            {
                Keyword = keyword,
                Location = location,
                Category = category,
                Jobs = await _jobs.SearchAsync(keyword ?? "", location ?? "", category ?? "")
            });

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

        [HttpPost("documents/upload")]
        [SwaggerOperation(Summary = "Upload a document", Tags = new[] { "Documents" })]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadRequest request)
        {
            var userId = GetUserId();
            var c = await _citizen.GetByUserIdAsync(userId);
            if (c == null) return NotFound(new { Message = "Profile not found." });

            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { Message = "No file selected." });

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.File.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                var dbFilePath = $"/uploads/documents/{uniqueFileName}";
                var (ok, msg) = await _docs.UploadAsync(c.Id, request.DocumentType, request.File.FileName, dbFilePath);

                if (ok)
                {
                    var allDocs = await _docs.GetByCitizenAsync(c.Id);
                    return Ok(allDocs.OrderByDescending(d => d.UploadedDate).FirstOrDefault());
                }

                return BadRequest(new { Message = msg });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Upload failed: {ex.Message}" });
            }
        }

        [HttpGet("programs")]
        [SwaggerOperation(Summary = "Get available programs", Tags = new[] { "Programs" })]
        public async Task<IActionResult> GetPrograms()
        {
            return Ok(await _programs.GetAllAsync());
        }

        [HttpGet("benefits")]
        [SwaggerOperation(Summary = "Get my benefits", Tags = new[] { "Benefits" })]
        public async Task<IActionResult> GetBenefits()
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });
            return Ok(await _benefits.GetByCitizenAsync(c.Id));
        }

        [HttpPost("benefits/apply/{programId}")]
        [SwaggerOperation(Summary = "Apply for a benefit program", Tags = new[] { "Benefits" })]
        public async Task<IActionResult> ApplyBenefit(int programId)
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });

            var (ok, msg) = await _benefits.ApplyAsync(c.Id, programId);
            return ok ? Ok(new { Message = "Application submitted!" }) : BadRequest(new { Message = msg });
        }

        [HttpGet("trainings")]
        [SwaggerOperation(Summary = "Get available trainings + my enrollments", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> GetTrainings()
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });

            var all = await _trainings.GetAllTrainingsAsync();
            var enrolled = await _trainings.GetEnrollmentsByCitizenAsync(c.Id);

            return Ok(new
            {
                Available = all.Where(t => t.Status == "Active"),
                Enrollments = enrolled
            });
        }

        [HttpPost("enroll/{id}")]
        [SwaggerOperation(Summary = "Enroll in a training", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> Enroll(int id)
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });

            var (ok, msg) = await _trainings.EnrollAsync(c.Id, id);
            return ok ? Ok(new { Message = "Enrolled!" }) : BadRequest(new { Message = msg });
        }

        [HttpPost("unenroll/{id}")]
        [SwaggerOperation(Summary = "Unenroll from a training", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> Unenroll(int id)
        {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new { Message = "Profile not found." });

            var (ok, msg) = await _trainings.UnenrollAsync(c.Id, id);
            return ok ? Ok(new { Message = "Unenrolled!" }) : BadRequest(new { Message = msg });
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