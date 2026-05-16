using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Data; // Required for ApplicationDbContext
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Controllers
{
    [Route("api/employer")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class EmployerController : ControllerBase
    {
        private readonly IEmployerService _employer;
        private readonly IJobService _jobs;
        private readonly INotificationService _notifications;
        private readonly ApplicationDbContext _context; // Required to query CitizenDocuments

        public EmployerController(
            IEmployerService employer,
            IJobService jobs,
            INotificationService notifications,
            ApplicationDbContext context) // Inject context here
        {
            _employer = employer;
            _jobs = jobs;
            _notifications = notifications;
            _context = context;
        }

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
        public async Task<IActionResult> GetProfile()
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            return e == null ? NotFound(new { Message = "Profile not found." }) : Ok(e);
        }

        [HttpPost("profile/register")]
        public async Task<IActionResult> Register([FromBody] Employer model)
        {
            var (ok, msg) = await _employer.RegisterAsync(GetUserId(), model);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] Employer model)
        {
            var userId = GetUserId();
            var e = await _employer.GetByUserIdAsync(userId);

            // If it's a brand new employer, initialize the record cleanly so it can be saved
            if (e == null)
            {
                // 🚨 CRITICAL FIX: Strip away any incoming tracked primary keys so Entity Framework 
                // knows it must generate a clean new auto-incremented Row inside the SQL table.
                var newEmployer = new Employer
                {
                    Id = 0, // Force identity generation
                    UserId = userId,
                    CompanyName = model.CompanyName ?? "New Company",
                    Industry = model.Industry ?? "Unassigned",
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber,
                    Website = model.Website,
                    Description = model.Description,
                    Status = "Pending"
                };

                var (registerOk, registerMsg) = await _employer.RegisterAsync(userId, newEmployer);
                return registerOk
                    ? Ok(new { Message = "Profile initialized.", Employer = newEmployer })
                    : BadRequest(new { Message = registerMsg });
            }

            // Otherwise, map fields safely for the existing employer
            e.CompanyName = model.CompanyName;
            e.Industry = model.Industry;
            e.Address = model.Address;
            e.PhoneNumber = model.PhoneNumber;
            e.Website = model.Website;
            e.Description = model.Description;

            var (ok, msg) = await _employer.UpdateProfileAsync(e);
            return ok
                ? Ok(new { Message = "Profile updated.", Employer = e })
                : BadRequest(new { Message = msg });
        }

        // ══════════════ DOCUMENT MANAGEMENT ══════════════

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments()
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            if (e == null) return NotFound(new { Message = "Profile not found." });

            // 1. Fetch documents from the database
            var docs = await _employer.GetDocumentsAsync(e.Id);

            // 2. Get the current backend URL (e.g., https://localhost:7003)
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // 3. Prepend the backend URL to the relative file paths
            foreach (var doc in docs)
            {
                if (!string.IsNullOrEmpty(doc.FileURL) && doc.FileURL.StartsWith("/uploads"))
                {
                    doc.FileURL = $"{baseUrl}{doc.FileURL}";
                }
            }

            return Ok(docs);
        }

        [HttpPost("documents")]
        [Consumes("multipart/form-data")]
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

        // ══════════════ JOB MANAGEMENT ══════════════

        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs()
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            return e == null ? NotFound() : Ok(await _employer.GetJobsAsync(e.Id));
        }

        [HttpGet("jobs/{id}")]
        public async Task<IActionResult> GetJobById(int id)
        {
            var job = await _jobs.GetByIdAsync(id);
            return job == null ? NotFound() : Ok(job);
        }

        [HttpPost("jobs")]
        public async Task<IActionResult> PostJob([FromBody] JobOpening job)
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            if (e == null) return NotFound(new { Message = "Profile not found." });

            job.EmployerId = e.Id;
            var (ok, msg) = await _jobs.CreateAsync(job);
            return ok ? Ok(new { Message = msg, Job = job }) : BadRequest(new { Message = msg });
        }

        [HttpPut("jobs/{id}")]
        public async Task<IActionResult> UpdateJob(int id, [FromBody] JobOpening job)
        {
            job.Id = id;
            var (ok, msg) = await _jobs.UpdateAsync(job);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("jobs/{jobId}/close")]
        public async Task<IActionResult> CloseJob(int jobId)
        {
            var (ok, msg) = await _jobs.CloseAsync(jobId);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // ══════════════ APPLICATIONS ══════════════

        [HttpGet("applications")]
        public async Task<IActionResult> GetApplications()
        {
            var e = await _employer.GetByUserIdAsync(GetUserId());
            return e == null ? NotFound() : Ok(await _employer.GetApplicationsReceivedAsync(e.Id));
        }

        [HttpGet("applications/{appId}")]
        public async Task<IActionResult> GetApplicationDetails(int appId)
        {
            var app = await _employer.GetApplicationDetailsAsync(appId);
            if (app == null) return NotFound();

            var resume = await _context.CitizenDocuments
                .Where(d => d.CitizenId == app.CitizenId
                         && d.DocumentType == "Resume")
                .OrderByDescending(d => d.UploadedDate)
                .FirstOrDefaultAsync();

            if (resume != null && !string.IsNullOrEmpty(resume.FilePath))
            {
                string citizenApiBaseUrl = "https://localhost:7002/";

                // 1. Extract ONLY the file name, ignoring any old folders in the database
                string fileName = Path.GetFileName(resume.FilePath);

                // 2. Force the path to point to your new documents folder
                string correctPath = $"uploads/documents/{fileName}";

                // 3. Combine them
                app.ResumeUrl = $"{citizenApiBaseUrl}{correctPath}";
            }

            return Ok(app);
        }

        [HttpPut("applications/{appId}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int appId, [FromQuery] string status, [FromBody] string? notes)
        {
            var (ok, msg) = await _employer.UpdateApplicationStatusAsync(appId, status, notes);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var uid = GetUserId();
            var n = await _notifications.GetByUserAsync(uid);
            await _notifications.MarkAllReadAsync(uid);
            return Ok(n);
        }
    }
}