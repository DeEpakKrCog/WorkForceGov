using Microsoft.AspNetCore.Mvc;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;
using WorkForceGovProject.Interfaces.Services;

namespace WorkForceGovProject.Controllers
{
    [Route("Citizen")]
    public class CitizenController : Controller
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
        {
            _citizen = citizen; _jobs = jobs; _apps = apps;
            _docs = docs; _benefits = benefits; _notifications = notifications;
            _programs = programs; _trainings = trainings;
        }

        private int? UserId => HttpContext.Session.GetInt32("UserId");

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null)
            {
                await _citizen.CreateProfileAsync(UserId.Value,
                    HttpContext.Session.GetString("UserName") ?? "User",
                    HttpContext.Session.GetString("UserEmail") ?? "");
            }
            var model = await _citizen.GetDashboardAsync(UserId.Value);
            return View(model);
        }

        [Route("Profile")]
        public async Task<IActionResult> Profile()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            return View(citizen);
        }

        [HttpPost, Route("Profile")]
        public async Task<IActionResult> Profile(Citizen model)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return NotFound();

            citizen.FullName = model.FullName;
            citizen.DOB = model.DOB;
            citizen.Gender = model.Gender;
            citizen.Address = model.Address;
            citizen.PhoneNumber = model.PhoneNumber;

            await _citizen.UpdateProfileAsync(citizen);
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        [Route("JobSearch")]
        public async Task<IActionResult> JobSearch(string? keyword, string? location, string? category)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var jobs = await _jobs.SearchAsync(keyword, location, category);
            var model = new JobSearchViewModel
            {
                Keyword = keyword,
                Location = location,
                Category = category,
                Jobs = jobs.ToList()
            };
            return View(model);
        }

        [HttpPost, Route("Apply/{jobId}")]
        public async Task<IActionResult> Apply(int jobId, string? coverLetter)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");

            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return RedirectToAction("Dashboard");

            // 1. Just check if a Resume exists at all
            var docs = await _citizen.GetDocumentsAsync(citizen.Id);
            bool hasResume = docs.Any(d => d.DocumentType.ToLower().Contains("resume"));

            if (!hasResume)
            {
                TempData["ErrorMessage"] = "You must upload a Resume before applying.";
                return RedirectToAction("Documents");
            }

            // 2. Attempt the application
            var (ok, msg) = await _citizen.ApplyForJobAsync(citizen.Id, jobId, coverLetter);

            if (ok)
            {
                TempData["SuccessMessage"] = "Application submitted successfully!";
                return RedirectToAction("JobApplications");
            }
            else
            {
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("JobSearch");
            }
        }


        [Route("JobApplications")]
        public async Task<IActionResult> JobApplications()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return RedirectToAction("Dashboard");
            var apps = await _apps.GetByCitizenAsync(citizen.Id);
            return View(apps);
        }

        [HttpPost, Route("WithdrawApplication/{id}")]
        public async Task<IActionResult> WithdrawApplication(int id)
        {
            var (success, msg) = await _apps.WithdrawAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("JobApplications");
        }

        [Route("Documents")]
        public async Task<IActionResult> Documents()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return RedirectToAction("Dashboard");
            var docs = await _docs.GetByCitizenAsync(citizen.Id);
            return View(docs);
        }

        [HttpPost, Route("UploadDocument")]
        public async Task<IActionResult> UploadDocument(string documentType, IFormFile file)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null || file == null) return RedirectToAction("Documents");

            if (file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("Documents");
            }

            // Save file to wwwroot/uploads/citizen-docs/ so employers can download it
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "citizen-docs");
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = $"{citizen.Id}_{documentType}_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(file.FileName)}";
            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(physicalPath, FileMode.Create))
                await file.CopyToAsync(stream);

            var fileUrl = $"/uploads/citizen-docs/{uniqueFileName}";
            var (success, msg) = await _docs.UploadAsync(citizen.Id, documentType, file.FileName, fileUrl);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Documents");
        }

        [Route("Benefits")]
        public async Task<IActionResult> Benefits()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return RedirectToAction("Dashboard");
            var benefits = await _benefits.GetByCitizenAsync(citizen.Id);
            var allPrograms = await _programs.GetAllProgramsAsync();
            var enrolledProgramIds = benefits.Select(b => b.ProgramId).ToHashSet();
            ViewBag.ActivePrograms = allPrograms.Where(p => p.Status == "Active").ToList();
            ViewBag.EnrolledProgramIds = enrolledProgramIds;
            return View(benefits);
        }

        [HttpPost, Route("ApplyBenefit/{programId}")]
        public async Task<IActionResult> ApplyBenefit(int programId)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return RedirectToAction("Dashboard");

            // Check not already enrolled
            var existing = await _benefits.GetByCitizenAsync(citizen.Id);
            if (existing.Any(b => b.ProgramId == programId))
            {
                TempData["ErrorMessage"] = "You have already applied for this program.";
                return RedirectToAction("Benefits");
            }

            var program = await _programs.GetByIdAsync(programId);
            if (program == null || program.Status != "Active")
            {
                TempData["ErrorMessage"] = "Program is not available.";
                return RedirectToAction("Benefits");
            }

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
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Successfully applied for '{program.ProgramName}'. Awaiting approval."
                : msg;
            return RedirectToAction("Benefits");
        }

        [Route("Trainings")]
        public async Task<IActionResult> Trainings()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return RedirectToAction("Dashboard");

            var allTrainings = await _trainings.GetAllTrainingsAsync();
            var myEnrollments = await _trainings.GetEnrollmentsByCitizenAsync(citizen.Id);
            var enrolledIds = myEnrollments.Select(e => e.TrainingId).ToHashSet();

            ViewBag.MyEnrollments = myEnrollments.ToList();
            ViewBag.EnrolledIds = enrolledIds;
            return View(allTrainings.Where(t => t.Status == "Active").ToList());
        }

        [HttpPost, Route("Enroll/{trainingId}")]
        public async Task<IActionResult> Enroll(int trainingId)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return RedirectToAction("Dashboard");

            var (success, msg) = await _trainings.EnrollAsync(citizen.Id, trainingId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Trainings");
        }

        [HttpPost, Route("Unenroll/{trainingId}")]
        public async Task<IActionResult> Unenroll(int trainingId)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var citizen = await _citizen.GetByUserIdAsync(UserId.Value);
            if (citizen == null) return RedirectToAction("Dashboard");

            var (success, msg) = await _trainings.UnenrollAsync(citizen.Id, trainingId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Trainings");
        }

        [Route("Notifications")]
        public async Task<IActionResult> Notifications()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var notifs = await _notifications.GetByUserAsync(UserId.Value);
            await _notifications.MarkAllReadAsync(UserId.Value);
            return View(notifs);
        }
    }
}
