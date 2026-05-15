using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class ProgramManagerController : ControllerBase
    {
        private readonly IProgramManagerService _pm;
        private readonly IProgramService _programs;
        private readonly ITrainingService _trainings;
        private readonly IResourceService _resources;
        private readonly INotificationService _notifications;

        public ProgramManagerController(
            IProgramManagerService pm,
            IProgramService programs,
            ITrainingService trainings,
            IResourceService resources,
            INotificationService notifications)
        {
            _pm = pm;
            _programs = programs;
            _trainings = trainings;
            _resources = resources;
            _notifications = notifications;
        }

        private int GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier);
            if (c != null && int.TryParse(c.Value, out int j))
                return j;

            throw new UnauthorizedAccessException("User ID not found in token.");
        }

        // ── DASHBOARD ───────────────────────────────────────────────────────
        [HttpGet("dashboard")]
        [SwaggerOperation(Summary = "Get Program Manager dashboard", Tags = new[] { "Dashboard" })]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var userId = GetUserId();
                var dashboard = await _pm.GetEnrichedDashboardAsync(userId);
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error fetching dashboard: {ex.Message}" });
            }
        }

        // ── BENEFIT APPROVAL WORKFLOW ───────────────────────────────────────

        // 🚨 NEW: Gets all benefits (what Angular is asking for)
        [HttpGet("benefits")]
        [SwaggerOperation(Summary = "Get all benefit applications", Tags = new[] { "Benefit Approval" })]
        public async Task<IActionResult> GetAllBenefits() =>
            Ok(await _pm.GetAllBenefitsAsync());

        [HttpGet("benefits/pending")]
        [SwaggerOperation(Summary = "Get pending benefit applications", Tags = new[] { "Benefit Approval" })]
        public async Task<IActionResult> GetPendingBenefits() =>
            Ok(await _pm.GetPendingBenefitsAsync());

        [HttpPut("benefits/{id}/approve")]
        [SwaggerOperation(Summary = "Approve a benefit — set amount and notify citizen", Tags = new[] { "Benefit Approval" })]
        public async Task<IActionResult> ApproveBenefit(int id, [FromQuery] decimal amount)
        {
            var (ok, msg) = await _pm.ApproveBenefitAsync(id, GetUserId(), amount, _notifications);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpPut("benefits/{id}/reject")]
        [SwaggerOperation(Summary = "Reject a benefit application and notify citizen", Tags = new[] { "Benefit Approval" })]
        public async Task<IActionResult> RejectBenefit(int id, [FromBody] string reason)
        {
            var (ok, msg) = await _pm.RejectBenefitAsync(id, GetUserId(), reason, _notifications);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // ── PROGRAMS ────────────────────────────────────────────────────────
        [HttpGet("programs")]
        [SwaggerOperation(Summary = "Get all employment programs", Tags = new[] { "Programs" })]
        public async Task<IActionResult> GetPrograms() =>
            Ok(await _programs.GetAllProgramsAsync());

        [HttpGet("programs/{id}")]
        [SwaggerOperation(Summary = "Get a program by ID", Tags = new[] { "Programs" })]
        public async Task<IActionResult> GetProgram(int id)
        {
            var p = await _programs.GetByIdAsync(id);
            return p == null ? NotFound() : Ok(p);
        }

        [HttpPost("programs")]
        [SwaggerOperation(Summary = "Create a new employment program", Tags = new[] { "Programs" })]
        public async Task<IActionResult> CreateProgram([FromBody] EmploymentProgram program)
        {
            var (ok, msg) = await _programs.CreateAsync(program);
            return ok ? Ok(new { Message = msg, Program = program }) : BadRequest(new { Message = msg });
        }

        [HttpPut("programs/{id}")]
        [SwaggerOperation(Summary = "Update a program", Tags = new[] { "Programs" })]
        public async Task<IActionResult> UpdateProgram(int id, [FromBody] EmploymentProgram program)
        {
            program.Id = id;
            var (ok, msg) = await _programs.UpdateAsync(program);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        [HttpDelete("programs/{id}")]
        [SwaggerOperation(Summary = "Delete a program", Tags = new[] { "Programs" })]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            var (ok, msg) = await _programs.DeleteAsync(id);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // ── TRAININGS ───────────────────────────────────────────────────────
        // ── TRAININGS ───────────────────────────────────────────────────────
        [HttpGet("trainings")]
        [SwaggerOperation(Summary = "Get all trainings", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> GetTrainings() =>
            Ok(await _trainings.GetAllTrainingsAsync());

        // 🚨 ADDED: Get a single training by ID (Fixes the "Failed to load data" error on the Edit page)
        [HttpGet("trainings/{id}")]
        [SwaggerOperation(Summary = "Get a training by ID", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> GetTraining(int id)
        {
            var t = await _trainings.GetByIdAsync(id);
            return t == null ? NotFound() : Ok(t);
        }

        [HttpPost("trainings")]
        [SwaggerOperation(Summary = "Create a new training session", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> CreateTraining([FromBody] Training training)
        {
            var (ok, msg) = await _trainings.CreateAsync(training);
            return ok ? Ok(new { Message = msg, Training = training }) : BadRequest(new { Message = msg });
        }

        [HttpPut("trainings/{id}")]
        [SwaggerOperation(Summary = "Update a training session", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> UpdateTraining(int id, [FromBody] Training training)
        {
            training.Id = id;
            var (ok, msg) = await _trainings.UpdateAsync(training);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

        // 🚨 ADDED: Delete a training (Makes your new Delete button work)
        [HttpDelete("trainings/{id}")]
        [SwaggerOperation(Summary = "Delete a training session", Tags = new[] { "Trainings" })]
        public async Task<IActionResult> DeleteTraining(int id)
        {
            var (ok, msg) = await _trainings.DeleteAsync(id);
            return ok ? Ok(new { Message = msg }) : BadRequest(new { Message = msg });
        }

    }
}