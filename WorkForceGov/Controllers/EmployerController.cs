using Microsoft.AspNetCore.Mvc;
using WorkForceGovProject.Models;
using WorkForceGovProject.Interfaces.Services;

namespace WorkForceGovProject.Controllers
{
    [Route("Employer")]
    public class EmployerController : Controller
    {
        private readonly IEmployerService _employer;
        private readonly IJobService _jobs;
        private readonly IApplicationService _apps;
        private readonly INotificationService _notifications;
        private readonly ISystemLogService _logs;
        private readonly IDocumentService _docs;

        public EmployerController(IEmployerService employer, IJobService jobs,
            IApplicationService apps, INotificationService notifications, ISystemLogService logs,
            IDocumentService docs)
        {
            _employer = employer; _jobs = jobs; _apps = apps;
            _notifications = notifications; _logs = logs; _docs = docs;
        }

        private int? UserId => HttpContext.Session.GetInt32("UserId");

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            if (emp == null) return RedirectToAction("Register");
            var model = await _employer.GetDashboardAsync(UserId.Value);
            return View(model);
        }

        [HttpGet, Route("Register")]
        public IActionResult Register()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost, Route("Register")]
        public async Task<IActionResult> Register(Employer model)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _employer.RegisterEmployerAsync(UserId.Value, model);
            if (success)
            {
                await _logs.LogAsync(UserId.Value, "EmployerRegistered", model.CompanyName);
                TempData["SuccessMessage"] = "Company registered! Please upload your Business License or PAN Card to get verified.";
                return RedirectToAction("UploadDocuments");
            }
            TempData["ErrorMessage"] = msg;
            return View(model);
        }

        // ── UPLOAD DOCUMENTS (required before posting jobs) ──────────────
        [HttpGet, Route("UploadDocuments")]
        public async Task<IActionResult> UploadDocuments()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            if (emp == null) return RedirectToAction("Register");
            var docs = await _employer.GetDocumentsAsync(emp.Id);
            ViewBag.Employer = emp;
            return View(docs);
        }

        [HttpPost, Route("UploadDocuments")]
        public async Task<IActionResult> UploadDocuments(string docType, IFormFile file)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            if (emp == null) return RedirectToAction("Register");

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("UploadDocuments");
            }

            // Save file to wwwroot/uploads/employer-docs/
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "employer-docs");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{emp.Id}_{docType}_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var fileUrl = $"/uploads/employer-docs/{fileName}";
            var (success, msg, _) = await _employer.UploadDocumentAsync(emp.Id, docType, fileUrl);
            if (success) await _logs.LogAsync(UserId.Value, "EmployerDocumentUploaded", docType);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"'{docType}' uploaded successfully. Awaiting Labor Officer verification."
                : msg;
            return RedirectToAction("UploadDocuments");
        }

        [Route("Profile")]
        public async Task<IActionResult> Profile()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            return View(emp);
        }

        [HttpPost, Route("Profile")]
        public async Task<IActionResult> Profile(Employer model)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            if (emp == null) return NotFound();
            emp.CompanyName = model.CompanyName; emp.Industry = model.Industry;
            emp.Address = model.Address; emp.ContactInfo = model.ContactInfo;
            await _employer.UpdateProfileAsync(emp);
            TempData["SuccessMessage"] = "Profile updated.";
            return RedirectToAction("Profile");
        }

        [HttpGet, Route("CreateJob")]
        public async Task<IActionResult> CreateJob()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            if (emp == null) return RedirectToAction("Register");
            if (emp.Status != "Verified")
            {
                TempData["ErrorMessage"] = "Your account must be verified by the Labor Officer before posting jobs. Please upload your documents.";
                return RedirectToAction("UploadDocuments");
            }
            return View();
        }

        [HttpPost, Route("CreateJob")]
        public async Task<IActionResult> CreateJob(JobOpening model)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            if (emp == null) return RedirectToAction("Register");
            if (emp.Status != "Verified")
            {
                TempData["ErrorMessage"] = "Verification Required: Upload your documents first.";
                return RedirectToAction("UploadDocuments");
            }
            model.EmployerId = emp.Id;
            var (success, msg) = await _jobs.CreateAsync(model);
            if (success) await _logs.LogAsync(UserId.Value, "JobPosted", model.JobTitle);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ManageJobs");
        }

        [Route("ManageJobs")]
        public async Task<IActionResult> ManageJobs()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            if (emp == null) return RedirectToAction("Register");
            var jobs = await _jobs.GetByEmployerAsync(emp.Id);
            return View(jobs);
        }

        [HttpGet, Route("EditJob/{id}")]
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _jobs.GetByIdAsync(id);
            return job == null ? NotFound() : View(job);
        }

        [HttpPost, Route("EditJob/{id}")]
        public async Task<IActionResult> EditJob(int id, JobOpening model)
        {
            model.Id = id;
            var (success, msg) = await _jobs.UpdateAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ManageJobs");
        }

        [HttpPost, Route("CloseJob/{id}")]
        public async Task<IActionResult> CloseJob(int id)
        {
            var (success, msg) = await _jobs.CloseAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ManageJobs");
        }

        [Route("Applications")]
        public async Task<IActionResult> Applications()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var emp = await _employer.GetByUserIdAsync(UserId.Value);
            if (emp == null) return RedirectToAction("Register");
            var apps = await _apps.GetByEmployerAsync(emp.Id);
            return View(apps);
        }

        [Route("ApplicationDetails/{id}")]
        public async Task<IActionResult> ApplicationDetails(int id, bool reviewed = false)
        {
            var app = await _apps.GetWithDetailsAsync(id);
            if (app == null) return NotFound();

            // Fetch citizen documents directly via IDocumentService (same pattern as
            // Labor module) to guarantee a fresh DB query regardless of EF Core
            // navigation-property tracking state.
            if (app.Citizen != null)
                ViewBag.CitizenDocuments = (await _docs.GetByCitizenAsync(app.Citizen.Id)).ToList();
            else
                ViewBag.CitizenDocuments = new List<CitizenDocument>();

            ViewBag.Reviewed = reviewed;
            return View(app);
        }

        [HttpPost, Route("UpdateApplication/{id}")]
        public async Task<IActionResult> UpdateApplication(int id, string status, string? notes)
        {
            var (success, msg) = await _apps.UpdateStatusAsync(id, status, notes);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Applications");
        }

        [Route("Notifications")]
        public async Task<IActionResult> Notifications()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var notifs = await _notifications.GetByUserAsync(UserId.Value);
            return View(notifs);
        }
    }
}