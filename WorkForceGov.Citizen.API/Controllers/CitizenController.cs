using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Controllers
{
    [Route("api/citizen")]
    [ApiController]
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

        public CitizenController(ICitizenService citizen, IJobService jobs, IApplicationService apps,
            IDocumentService docs, IBenefitService benefits, INotificationService notifications,
            IProgramService programs, ITrainingService trainings)
        { _citizen=citizen; _jobs=jobs; _apps=apps; _docs=docs; _benefits=benefits; _notifications=notifications; _programs=programs; _trainings=trainings; }

        private int GetUserId() {
            var c = User.FindFirst(ClaimTypes.NameIdentifier);
            if (c != null && int.TryParse(c.Value, out int j)) return j;
            //return 0;
            if (Request.Headers.TryGetValue("X-User-Id", out var h) && int.TryParse(h, out int p)) return p;
            throw new UnauthorizedAccessException("Provide X-User-Id header.");
        }

        [HttpGet("dashboard")]
        [SwaggerOperation(Summary="Get citizen dashboard", Tags=new[]{"Dashboard"})]
        public async Task<IActionResult> GetDashboard() {
            var userId = GetUserId();
            if (await _citizen.GetByUserIdAsync(userId) == null)
                await _citizen.CreateProfileAsync(userId, "New User", "");  
            return Ok(await _citizen.GetDashboardAsync(userId));
        }

        [HttpGet("profile")]
        [SwaggerOperation(Summary="Get citizen profile", Tags=new[]{"Profile"})]
        public async Task<IActionResult> GetProfile() {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            return c == null ? NotFound(new{Message="Profile not found."}) : Ok(c);
        }

        [HttpPut("profile")]
        [SwaggerOperation(Summary="Update citizen profile", Tags=new[]{"Profile"})]
        public async Task<IActionResult> UpdateProfile([FromBody] WorkForceGovProject.Models.Citizen model) {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new{Message="Profile not found."});
            c.FullName=model.FullName; c.DOB=model.DOB; c.Gender=model.Gender; c.Address=model.Address; c.PhoneNumber=model.PhoneNumber;
            var (ok,msg) = await _citizen.UpdateProfileAsync(c);
            return ok ? Ok(new{Message="Profile updated.",Citizen=c}) : BadRequest(new{Message=msg});
        }

        [HttpGet("jobs/search")]
        [SwaggerOperation(Summary="Search job openings", Tags=new[]{"Jobs"})]
        public async Task<IActionResult> SearchJobs([FromQuery]string? keyword,[FromQuery]string? location,[FromQuery]string? category)
            => Ok(new{Keyword=keyword,Location=location,Category=category,Jobs=await _jobs.SearchAsync(keyword,location,category)});

        [HttpPost("jobs/{jobId}/apply")]
        [SwaggerOperation(Summary="Apply for a job", Tags=new[]{"Jobs"})]
        public async Task<IActionResult> ApplyForJob(int jobId, [FromBody]string? coverLetter) {
            var userId = GetUserId();
            var c = await _citizen.GetByUserIdAsync(userId);
            if (c == null) return NotFound(new{Message="Profile required."});
            var docs = await _citizen.GetDocumentsAsync(c.Id);
            if (!docs.Any(d => d.DocumentType.ToLower().Contains("resume")))
                return BadRequest(new{Message="Upload a Resume before applying."});
            var (ok,msg) = await _citizen.ApplyForJobAsync(c.Id, jobId, coverLetter);
            return ok ? Ok(new{Message="Application submitted!"}) : BadRequest(new{Message=msg});
        }

        [HttpGet("applications")]
        [SwaggerOperation(Summary="Get my applications", Tags=new[]{"Applications"})]
        public async Task<IActionResult> GetApplications() {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            return c == null ? NotFound() : Ok(await _apps.GetByCitizenAsync(c.Id));
        }

        [HttpPut("applications/{id}/withdraw")]
        [SwaggerOperation(Summary="Withdraw an application", Tags=new[]{"Applications"})]
        public async Task<IActionResult> WithdrawApplication(int id) {
            var (ok,msg) = await _apps.WithdrawAsync(id);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpGet("documents")]
        [SwaggerOperation(Summary="Get my documents", Tags=new[]{"Documents"})]
        public async Task<IActionResult> GetDocuments() {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            return c == null ? NotFound() : Ok(await _docs.GetByCitizenAsync(c.Id));
        }

        [HttpPost("documents")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary="Upload a document (Resume, ID, etc.)", Tags=new[]{"Documents"})]
        public async Task<IActionResult> UploadDocument([FromForm]string documentType, IFormFile file) {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound(new{Message="Profile not found."});
            if (file==null||file.Length==0) return BadRequest(new{Message="Select a valid file."});
            var folder = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads","citizen-docs");
            Directory.CreateDirectory(folder);
            var fn = $"{c.Id}_{documentType}_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
            using(var s = new FileStream(Path.Combine(folder,fn),FileMode.Create)) await file.CopyToAsync(s);
            var url = $"/uploads/citizen-docs/{fn}";
            var (ok,msg) = await _docs.UploadAsync(c.Id,documentType,file.FileName,url);
            return ok ? Ok(new{Message=msg,FileUrl=url}) : BadRequest(new{Message=msg});
        }

        [HttpGet("benefits")]
        [SwaggerOperation(Summary="Get benefits & available programs", Tags=new[]{"Benefits"})]
        public async Task<IActionResult> GetBenefits() {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound();
            var benefits = await _benefits.GetByCitizenAsync(c.Id);
            var programs = await _programs.GetAllProgramsAsync();
            return Ok(new{MyBenefits=benefits,ActivePrograms=programs.Where(p=>p.Status=="Active"),EnrolledProgramIds=benefits.Select(b=>b.ProgramId).ToHashSet()});
        }

        [HttpPost("benefits/{programId}/apply")]
        [SwaggerOperation(Summary="Apply for a benefit program", Tags=new[]{"Benefits"})]
        public async Task<IActionResult> ApplyBenefit(int programId) {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound();
            var existing = await _benefits.GetByCitizenAsync(c.Id);
            if (existing.Any(b=>b.ProgramId==programId)) return BadRequest(new{Message="Already applied."});
            var program = await _programs.GetByIdAsync(programId);
            if (program==null||program.Status!="Active") return BadRequest(new{Message="Program not available."});
            var benefit = new Benefit{CitizenId=c.Id,ProgramId=programId,BenefitType=program.ProgramType??"General",Amount=0,Status="Pending",BenefitDate=DateTime.Now,Description="Awaiting PM approval."};
            var (ok,msg) = await _benefits.CreateBenefitAsync(benefit);
            return ok ? Ok(new{Message=$"Applied for '{program.ProgramName}'. Awaiting approval."}) : BadRequest(new{Message=msg});
        }

        [HttpGet("trainings")]
        [SwaggerOperation(Summary="Get available trainings & my enrollments", Tags=new[]{"Trainings"})]
        public async Task<IActionResult> GetTrainings() {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound();
            var all = await _trainings.GetAllTrainingsAsync();
            var enrolled = await _trainings.GetEnrollmentsByCitizenAsync(c.Id);
            return Ok(new{AvailableTrainings=all.Where(t=>t.Status=="Active"),MyEnrollments=enrolled,EnrolledIds=enrolled.Select(e=>e.TrainingId).ToHashSet()});
        }

        [HttpPost("trainings/{trainingId}/enroll")]
        [SwaggerOperation(Summary="Enroll in a training", Tags=new[]{"Trainings"})]
        public async Task<IActionResult> EnrollTraining(int trainingId) {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound();
            var (ok,msg) = await _trainings.EnrollAsync(c.Id,trainingId);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpPost("trainings/{trainingId}/unenroll")]
        [SwaggerOperation(Summary="Unenroll from a training", Tags=new[]{"Trainings"})]
        public async Task<IActionResult> UnenrollTraining(int trainingId) {
            var c = await _citizen.GetByUserIdAsync(GetUserId());
            if (c == null) return NotFound();
            var (ok,msg) = await _trainings.UnenrollAsync(c.Id,trainingId);
            return ok ? Ok(new{Message=msg}) : BadRequest(new{Message=msg});
        }

        [HttpGet("complaints")]
        [SwaggerOperation(Summary="Get my complaints", Tags=new[]{"Complaints"})]
        public async Task<IActionResult> GetComplaints()
            => Ok(await _citizen.GetMyComplaintsAsync(GetUserId()));

        [HttpPost("complaints")]
        [SwaggerOperation(Summary="Raise a complaint against an employer", Tags=new[]{"Complaints"})]
        public async Task<IActionResult> RaiseComplaint([FromQuery]int employerId,[FromBody]string description) {
            var (ok,msg,complaint) = await _citizen.RaiseComplaintAsync(GetUserId(),employerId,description);
            return ok ? Ok(new{Message=msg,Complaint=complaint}) : BadRequest(new{Message=msg});
        }

        [HttpGet("notifications")]
        [SwaggerOperation(Summary="Get notifications (marks all read)", Tags=new[]{"Notifications"})]
        public async Task<IActionResult> GetNotifications() {
            var userId = GetUserId();
            var n = await _notifications.GetByUserAsync(userId);
            await _notifications.MarkAllReadAsync(userId);
            return Ok(n);
        }
    }
}
