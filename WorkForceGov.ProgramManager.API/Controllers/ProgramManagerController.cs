using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Controllers
{
    [Route("api/program-manager")]
    [ApiController]
    [Produces("application/json")]
    public class ProgramManagerController : ControllerBase
    {
        private readonly IProgramManagerService _pm;
        private readonly IProgramService _programs;
        private readonly ITrainingService _trainings;
        private readonly IResourceService _resources;
        private readonly INotificationService _notifications;

        public ProgramManagerController(IProgramManagerService pm, IProgramService programs,
            ITrainingService trainings, IResourceService resources, INotificationService notifications)
        { _pm=pm;_programs=programs;_trainings=trainings;_resources=resources;_notifications=notifications; }

        private int GetUserId() {
            var c=User.FindFirst(ClaimTypes.NameIdentifier);
            if(c!=null&&int.TryParse(c.Value,out int j)) return j;
            if(Request.Headers.TryGetValue("X-User-Id",out var h)&&int.TryParse(h,out int p)) return p;
            throw new UnauthorizedAccessException("Provide X-User-Id header.");
        }

        [HttpGet("dashboard")]
        [SwaggerOperation(Summary="Get Program Manager dashboard",Tags=new[]{"Dashboard"})]
        public async Task<IActionResult> GetDashboard() => Ok(await _pm.GetEnrichedDashboardAsync(GetUserId()));

        // ── BENEFIT APPROVAL WORKFLOW ───────────────────────────────────────
        [HttpGet("benefits/pending")]
        [SwaggerOperation(Summary="Get pending benefit applications",Tags=new[]{"Benefit Approval"})]
        public async Task<IActionResult> GetPendingBenefits() => Ok(await _pm.GetPendingBenefitsAsync());

        [HttpPut("benefits/{id}/approve")]
        [SwaggerOperation(Summary="Approve a benefit — set amount and notify citizen",Tags=new[]{"Benefit Approval"})]
        public async Task<IActionResult> ApproveBenefit(int id,[FromQuery]decimal amount) {
            var (ok,msg)=await _pm.ApproveBenefitAsync(id,GetUserId(),amount,_notifications);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        [HttpPut("benefits/{id}/reject")]
        [SwaggerOperation(Summary="Reject a benefit application and notify citizen",Tags=new[]{"Benefit Approval"})]
        public async Task<IActionResult> RejectBenefit(int id,[FromBody]string reason) {
            var (ok,msg)=await _pm.RejectBenefitAsync(id,GetUserId(),reason,_notifications);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        // ── PROGRAMS ────────────────────────────────────────────────────────
        [HttpGet("programs")]
        [SwaggerOperation(Summary="Get all employment programs",Tags=new[]{"Programs"})]
        public async Task<IActionResult> GetPrograms() => Ok(await _programs.GetAllProgramsAsync());

        [HttpGet("programs/{id}")]
        [SwaggerOperation(Summary="Get a program by ID",Tags=new[]{"Programs"})]
        public async Task<IActionResult> GetProgram(int id) {
            var p=await _programs.GetByIdAsync(id);
            return p==null?NotFound():Ok(p);
        }

        [HttpPost("programs")]
        [SwaggerOperation(Summary="Create a new employment program",Tags=new[]{"Programs"})]
        public async Task<IActionResult> CreateProgram([FromBody]EmploymentProgram program) {
            var (ok,msg)=await _programs.CreateAsync(program);
            return ok?Ok(new{Message=msg,Program=program}):BadRequest(new{Message=msg});
        }

        [HttpPut("programs/{id}")]
        [SwaggerOperation(Summary="Update a program",Tags=new[]{"Programs"})]
        public async Task<IActionResult> UpdateProgram(int id,[FromBody]EmploymentProgram program) {
            program.Id=id;
            var (ok,msg)=await _programs.UpdateAsync(program);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        [HttpDelete("programs/{id}")]
        [SwaggerOperation(Summary="Delete a program",Tags=new[]{"Programs"})]
        public async Task<IActionResult> DeleteProgram(int id) {
            var (ok,msg)=await _programs.DeleteAsync(id);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        // ── TRAININGS ───────────────────────────────────────────────────────
        [HttpGet("trainings")]
        [SwaggerOperation(Summary="Get all trainings",Tags=new[]{"Trainings"})]
        public async Task<IActionResult> GetTrainings() => Ok(await _trainings.GetAllTrainingsAsync());

        [HttpPost("trainings")]
        [SwaggerOperation(Summary="Create a new training session",Tags=new[]{"Trainings"})]
        public async Task<IActionResult> CreateTraining([FromBody]Training training) {
            var (ok,msg)=await _trainings.CreateAsync(training);
            return ok?Ok(new{Message=msg,Training=training}):BadRequest(new{Message=msg});
        }

        [HttpPut("trainings/{id}")]
        [SwaggerOperation(Summary="Update a training session",Tags=new[]{"Trainings"})]
        public async Task<IActionResult> UpdateTraining(int id,[FromBody]Training training) {
            training.Id=id;
            var (ok,msg)=await _trainings.UpdateAsync(training);
            return ok?Ok(new{Message=msg}):BadRequest(new{Message=msg});
        }

        // ── RESOURCES ───────────────────────────────────────────────────────
        [HttpGet("resources")]
        [SwaggerOperation(Summary="Get all resources",Tags=new[]{"Resources"})]
        public async Task<IActionResult> GetResources() => Ok(await _resources.GetAllAsync());

        [HttpPost("resources")]
        [SwaggerOperation(Summary="Create a resource",Tags=new[]{"Resources"})]
        public async Task<IActionResult> CreateResource([FromBody]Resource resource) {
            var (ok,msg)=await _resources.CreateAsync(resource);
            return ok?Ok(new{Message=msg,Resource=resource}):BadRequest(new{Message=msg});
        }

        [HttpGet("notifications")]
        [SwaggerOperation(Summary="Get notifications",Tags=new[]{"Notifications"})]
        public async Task<IActionResult> GetNotifications() {
            var uid=GetUserId(); var n=await _notifications.GetByUserAsync(uid);
            await _notifications.MarkAllReadAsync(uid); return Ok(n);
        }
    }
}
