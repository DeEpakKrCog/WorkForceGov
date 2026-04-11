using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkForceGovProject.Models;
using WorkForceGovProject.Interfaces.Services;

namespace WorkForceGov.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Uncomment this once you implement JWT Authentication
    public class CitizensController : ControllerBase
    {
        private readonly ICitizenService _citizen;
        private readonly IJobService _jobs;
        private readonly IApplicationService _apps;
        private readonly IDocumentService _docs;
        private readonly IBenefitService _benefits;
        private readonly INotificationService _notifications;
        private readonly IProgramService _programs;
        private readonly ITrainingService _trainings;

        public CitizensController(
            ICitizenService citizen, IJobService jobs, IApplicationService apps,
            IDocumentService docs, IBenefitService benefits, INotificationService notifications,
            IProgramService programs, ITrainingService trainings)
        {
            _citizen = citizen; _jobs = jobs; _apps = apps;
            _docs = docs; _benefits = benefits; _notifications = notifications;
            _programs = programs; _trainings = trainings;
        }

        /// <summary>
        /// Helper method to get the User ID from the JWT Token.
        /// For testing without auth, you can temporarily hardcode a return value (e.g., return 1;).
        /// </summary>
        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }
            // Fallback for easy Postman testing if JWT isn't set up yet:
            // Send a header named "X-User-Id" with the user's ID.
            if (Request.Headers.TryGetValue("X-User-Id", out var headerId) && int.TryParse(headerId, out int parsedId))
            {
                return parsedId;
            }

            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // ── DASHBOARD ──

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);

            if (citizen == null)
            {
                // In an API, you might require the frontend to call a specific setup endpoint first, 
                // but we can auto-create the profile here just like the MVC version.
                await _citizen.CreateProfileAsync(userId, "New User", "");
            }

            var model = await _citizen.GetDashboardAsync(userId);
            return Ok(model);
        }

        // ── PROFILE ──

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound("Citizen profile not found.");

            return Ok(citizen);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] Citizen model)
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound("Citizen profile not found.");

            citizen.FullName = model.FullName;
            citizen.DOB = model.DOB;
            citizen.Gender = model.Gender;
            citizen.Address = model.Address;
            citizen.PhoneNumber = model.PhoneNumber;

            var (success, msg) = await _citizen.UpdateProfileAsync(citizen);
            if (!success) return BadRequest(msg);

            return Ok(new { Message = "Profile updated successfully.", Citizen = citizen });
        }

        // ── JOBS & APPLICATIONS ──

        [HttpGet("jobs/search")]
        public async Task<IActionResult> SearchJobs([FromQuery] string? keyword, [FromQuery] string? location, [FromQuery] string? category)
        {
            var jobs = await _jobs.SearchAsync(keyword, location, category);
            return Ok(new
            {
                Keyword = keyword,
                Location = location,
                Category = category,
                Jobs = jobs
            });
        }

        [HttpPost("jobs/{jobId}/apply")]
        public async Task<IActionResult> ApplyForJob(int jobId, [FromBody] string? coverLetter)
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound("Profile required to apply.");

            // Check if Resume exists
            var docs = await _citizen.GetDocumentsAsync(citizen.Id);
            bool hasResume = docs.Any(d => d.DocumentType.ToLower().Contains("resume"));

            if (!hasResume)
                return BadRequest("You must upload a Resume before applying.");

            var (ok, msg) = await _citizen.ApplyForJobAsync(citizen.Id, jobId, coverLetter);

            if (ok)
                return Ok(new { Message = "Application submitted successfully!" });

            return BadRequest(msg);
        }

        [HttpGet("applications")]
        public async Task<IActionResult> GetJobApplications()
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound();

            var apps = await _apps.GetByCitizenAsync(citizen.Id);
            return Ok(apps);
        }

        [HttpPut("applications/{id}/withdraw")]
        public async Task<IActionResult> WithdrawApplication(int id)
        {
            var (success, msg) = await _apps.WithdrawAsync(id);
            if (success) return Ok(new { Message = msg });

            return BadRequest(msg);
        }

        // ── DOCUMENTS ──

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments()
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound();

            var docs = await _docs.GetByCitizenAsync(citizen.Id);
            return Ok(docs);
        }

        [HttpPost("documents")]
        public async Task<IActionResult> UploadDocument([FromForm] string documentType, IFormFile file)
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound("Citizen profile not found.");

            if (file == null || file.Length == 0)
                return BadRequest("Please select a valid file to upload.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "citizen-docs");
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = $"{citizen.Id}_{documentType}_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(file.FileName)}";
            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/citizen-docs/{uniqueFileName}";
            var (success, msg) = await _docs.UploadAsync(citizen.Id, documentType, file.FileName, fileUrl);

            if (success) return Ok(new { Message = msg, FileUrl = fileUrl });

            return BadRequest(msg);
        }

        // ── BENEFITS ──

        [HttpGet("benefits")]
        public async Task<IActionResult> GetBenefits()
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound();

            var benefits = await _benefits.GetByCitizenAsync(citizen.Id);
            var allPrograms = await _programs.GetAllProgramsAsync();
            var enrolledProgramIds = benefits.Select(b => b.ProgramId).ToHashSet();

            var activePrograms = allPrograms.Where(p => p.Status == "Active").ToList();

            return Ok(new
            {
                MyBenefits = benefits,
                ActivePrograms = activePrograms,
                EnrolledProgramIds = enrolledProgramIds
            });
        }

        [HttpPost("benefits/{programId}/apply")]
        public async Task<IActionResult> ApplyBenefit(int programId)
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound();

            var existing = await _benefits.GetByCitizenAsync(citizen.Id);
            if (existing.Any(b => b.ProgramId == programId))
                return BadRequest("You have already applied for this program.");

            var program = await _programs.GetByIdAsync(programId);
            if (program == null || program.Status != "Active")
                return BadRequest("Program is not available.");

            var benefit = new Benefit
            {
                CitizenId = citizen.Id,
                ProgramId = programId,
                BenefitType = program.ProgramType ?? "General",
                Amount = 0,
                Status = "Pending",
                BenefitDate = DateTime.Now,
                Description = "Citizen enrollment request — awaiting Program Manager approval."
            };

            var (success, msg) = await _benefits.CreateBenefitAsync(benefit);
            if (success) return Ok(new { Message = $"Successfully applied for '{program.ProgramName}'. Awaiting approval." });

            return BadRequest(msg);
        }

        // ── TRAININGS ──

        [HttpGet("trainings")]
        public async Task<IActionResult> GetTrainings()
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound();

            var allTrainings = await _trainings.GetAllTrainingsAsync();
            var myEnrollments = await _trainings.GetEnrollmentsByCitizenAsync(citizen.Id);
            var enrolledIds = myEnrollments.Select(e => e.TrainingId).ToHashSet();

            return Ok(new
            {
                AvailableTrainings = allTrainings.Where(t => t.Status == "Active").ToList(),
                MyEnrollments = myEnrollments,
                EnrolledTrainingIds = enrolledIds
            });
        }

        [HttpPost("trainings/{trainingId}/enroll")]
        public async Task<IActionResult> EnrollTraining(int trainingId)
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound();

            var (success, msg) = await _trainings.EnrollAsync(citizen.Id, trainingId);
            if (success) return Ok(new { Message = msg });

            return BadRequest(msg);
        }

        [HttpPost("trainings/{trainingId}/unenroll")]
        public async Task<IActionResult> UnenrollTraining(int trainingId)
        {
            var userId = GetUserId();
            var citizen = await _citizen.GetByUserIdAsync(userId);
            if (citizen == null) return NotFound();

            var (success, msg) = await _trainings.UnenrollAsync(citizen.Id, trainingId);
            if (success) return Ok(new { Message = msg });

            return BadRequest(msg);
        }

        // ── NOTIFICATIONS ──

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = GetUserId();
            var notifs = await _notifications.GetByUserAsync(userId);

            // Mark as read when fetched
            await _notifications.MarkAllReadAsync(userId);

            return Ok(notifs);
        }
    }
}