using Microsoft.AspNetCore.Mvc;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;
using WorkForceGovProject.Interfaces.Services;

namespace WorkForceGovProject.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminDashboardService _dashboard;
        private readonly IAccountService _account;
        private readonly IReportService _reports;
        private readonly ISystemLogService _logs;

        public AdminController(IAdminDashboardService dashboard, IAccountService account, IReportService reports, ISystemLogService logs)
        { _dashboard = dashboard; _account = account; _reports = reports; _logs = logs; }

        private int? UserId => HttpContext.Session.GetInt32("UserId");

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var adminModel = await _dashboard.GetDashboardAsync();
            var viewModel = new DashboardViewModel
            {
                TotalUsers = adminModel.TotalUsers,
                RecentLogs = new List<SystemLog>(),
                RecentJobs = new List<JobOpening>(),
                Programs = new List<EmploymentProgram>()
            };
            return View(viewModel);
        }

        [Route("ManageUsers")]
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _account.GetAllUsersAsync();
            return View(new UserManageViewModel { Users = users.ToList() });
        }

        [HttpGet, Route("CreateUser")]
        public IActionResult CreateUser() => View();

        [HttpPost, Route("CreateUser")]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            var (success, msg) = await _account.CreateUserAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return success ? RedirectToAction("ManageUsers") : View(model);
        }

        [HttpGet, Route("EditUser/{id}")]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _account.GetByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost, Route("EditUser/{id}")]
        public async Task<IActionResult> EditUser(int id, User model)
        {
            var existing = await _account.GetByIdAsync(id);
            if (existing == null) return NotFound();
            existing.FullName = model.FullName; existing.Email = model.Email;
            existing.Role = model.Role; existing.Status = model.Status; existing.Phone = model.Phone;
            if (!string.IsNullOrEmpty(model.Password)) existing.Password = model.Password;
            var (success, msg) = await _account.UpdateUserAsync(existing);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ManageUsers");
        }

        [HttpPost, Route("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var (success, msg) = await _account.DeleteUserAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ManageUsers");
        }

        [Route("Reports")]
        public async Task<IActionResult> Reports()
        {
            var reports = await _reports.GetAllAsync();
            return View(reports);
        }

        [HttpPost, Route("GenerateReport")]
        public async Task<IActionResult> GenerateReport(string reportName, string reportType)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var report = new Report { ReportName = reportName, ReportType = reportType, GeneratedBy = UserId.Value, GeneratedDate = DateTime.Now, ReportContent = $"Auto-generated {reportType} report at {DateTime.Now:yyyy-MM-dd HH:mm}" };
            var (success, msg) = await _reports.GenerateAsync(report);
            if (success) await _logs.LogAsync(UserId.Value, "GenerateReport", reportType);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Reports");
        }

        [Route("SystemMonitoring")]
        public async Task<IActionResult> SystemMonitoring()
        {
            var logs = await _logs.GetRecentAsync(100);
            return View(logs);
        }
    }

    [Route("LaborOfficer")]
    public class LaborOfficerController : Controller
    {
        private readonly ILaborOfficerService _labor;
        private readonly IDocumentService _docs;
        private readonly IApplicationService _apps;
        private readonly IComplianceService _compliance;
        private readonly IAuditService _audits;
        private readonly IReportService _reports;
        private readonly IComplianceOfficerService _complianceOfficer;

        public LaborOfficerController(ILaborOfficerService labor, IDocumentService docs,
            IApplicationService apps, IComplianceService compliance, IAuditService audits,
            IReportService reports, IComplianceOfficerService complianceOfficer)
        {
            _labor = labor; _docs = docs; _apps = apps;
            _compliance = compliance; _audits = audits; _reports = reports;
            _complianceOfficer = complianceOfficer;
        }

        private int? UserId => HttpContext.Session.GetInt32("UserId");

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var model = await _labor.GetDashboardAsync(UserId.Value);
            return View(model);
        }

        [Route("DocumentVerifications")]
        public async Task<IActionResult> DocumentVerifications()
        {
            var citizenDocs = await _docs.GetPendingAsync();
            var employerDocs = await _complianceOfficer.GetPendingEmployerDocumentsAsync();
            ViewBag.EmployerDocuments = employerDocs.ToList();
            return View(citizenDocs);
        }

        [HttpPost, Route("VerifyDocument/{id}")]
        public async Task<IActionResult> VerifyDocument(int id)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _docs.VerifyAsync(id, UserId.Value);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("DocumentVerifications");
        }

        [HttpPost, Route("RejectDocument/{id}")]
        public async Task<IActionResult> RejectDocument(int id, string reason)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _docs.RejectAsync(id, UserId.Value, reason);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("DocumentVerifications");
        }

        [HttpPost, Route("VerifyEmployerDocument/{id}")]
        public async Task<IActionResult> VerifyEmployerDocument(int id)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _complianceOfficer.ApproveEmployerDocumentAsync(id, UserId.Value);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("DocumentVerifications");
        }

        [HttpPost, Route("RejectEmployerDocument/{id}")]
        public async Task<IActionResult> RejectEmployerDocument(int id, string reason)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _complianceOfficer.RejectEmployerDocumentAsync(id, UserId.Value, reason ?? "Does not meet requirements");
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("DocumentVerifications");
        }

        [Route("Applications")]
        public async Task<IActionResult> Applications()
        {
            var apps = await _apps.GetAllWithDetailsAsync();
            return View(apps);
        }

        [Route("Compliance")]
        public async Task<IActionResult> Compliance()
        {
            var records = await _compliance.GetAllAsync();
            return View(records);
        }

        [Route("Audits")]
        public async Task<IActionResult> Audits()
        {
            var audits = await _audits.GetAllAsync();
            return View(audits);
        }

        [Route("Reports")]
        public async Task<IActionResult> Reports()
        {
            var reports = await _reports.GetAllAsync();
            return View(reports);
        }

        [HttpPost, Route("GenerateReport")]
        public async Task<IActionResult> GenerateReport(string reportName, string reportType)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var report = new Report { ReportName = reportName, ReportType = reportType, GeneratedBy = UserId.Value, GeneratedDate = DateTime.Now };
            var (success, msg) = await _reports.GenerateAsync(report);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Reports");
        }
    }

    [Route("ComplianceOfficer")]
    public class ComplianceOfficerController : Controller
    {
        private readonly IComplianceOfficerService _complianceOfficer;
        private readonly IReportService _reports;

        public ComplianceOfficerController(IComplianceOfficerService complianceOfficer, IReportService reports)
        { _complianceOfficer = complianceOfficer; _reports = reports; }

        private int? UserId => HttpContext.Session.GetInt32("UserId");

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var model = await _complianceOfficer.GetDashboardAsync(UserId.Value);
            return View(model);
        }

        [Route("EmployerReview")]
        public async Task<IActionResult> EmployerReview()
        {
            var employers = await _complianceOfficer.GetAllEmployersAsync();
            return View(employers);
        }

        [Route("InvestigateComplaint")]
        public async Task<IActionResult> InvestigateComplaint()
        {
            var complaints = await _complianceOfficer.GetAllComplaintsAsync();
            return View(complaints);
        }

        [HttpPost, Route("ApproveDocument/{id}")]
        public async Task<IActionResult> ApproveDocument(int id)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _complianceOfficer.ApproveEmployerDocumentAsync(id, UserId.Value);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Dashboard");
        }

        [HttpPost, Route("RejectDocument/{id}")]
        public async Task<IActionResult> RejectDocument(int id, string reason)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _complianceOfficer.RejectEmployerDocumentAsync(id, UserId.Value, reason ?? "Does not meet requirements");
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Dashboard");
        }

        [HttpPost, Route("ResolveComplaint/{id}")]
        public async Task<IActionResult> ResolveComplaint(int id, string resolution)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _complianceOfficer.ResolveComplaintAsync(id, UserId.Value, resolution);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("InvestigateComplaint");
        }

        [HttpPost, Route("CreateComplianceRecord")]
        public async Task<IActionResult> CreateComplianceRecord(ComplianceRecord record)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            record.OfficerId = UserId.Value;
            await _complianceOfficer.CreateComplianceRecordAsync(record);
            TempData["SuccessMessage"] = "Compliance record created.";
            return RedirectToAction("Dashboard");
        }

        [Route("ComplianceReports")]
        public async Task<IActionResult> ComplianceReports()
        {
            var reports = await _reports.GetAllAsync();
            return View(reports);
        }
    }

    [Route("GovernmentAuditor")]
    public class GovernmentAuditorController : Controller
    {
        private readonly IAuditService _audits;
        private readonly IComplianceService _compliance;
        private readonly IProgramService _programs;
        private readonly IReportService _reports;
        private readonly INotificationService _notifications;

        public GovernmentAuditorController(IAuditService audits, IComplianceService compliance,
            IProgramService programs, IReportService reports, INotificationService notifications)
        {
            _audits = audits; _compliance = compliance; _programs = programs;
            _reports = reports; _notifications = notifications;
        }

        private int? UserId => HttpContext.Session.GetInt32("UserId");

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var model = await _audits.GetDashboardAsync(UserId.Value);
            return View(model);
        }

        [Route("ComplianceMonitoring")]
        public async Task<IActionResult> ComplianceMonitoring()
        {
            var records = await _compliance.GetAllAsync();
            return View(records);
        }

        [Route("WorkforcePrograms")]
        public async Task<IActionResult> WorkforcePrograms()
        {
            var programs = await _programs.GetAllAsync();
            return View(programs);
        }

        [HttpPost, Route("CreateAudit")]
        public async Task<IActionResult> CreateAudit(Audit audit)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            audit.OfficerId = UserId.Value;
            await _audits.CreateAsync(audit);
            TempData["SuccessMessage"] = "Audit created.";
            return RedirectToAction("Dashboard");
        }

        [Route("AuditReports")]
        public async Task<IActionResult> AuditReports()
        {
            var reports = await _reports.GetAllAsync();
            return View(reports);
        }

        [Route("AlertsAndNotifications")]
        public async Task<IActionResult> AlertsAndNotifications()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var notifs = await _notifications.GetByUserAsync(UserId.Value);
            return View(notifs);
        }
    }

    [Route("ProgramManager")]
    public class ProgramManagerController : Controller
    {
        private readonly IProgramService _programs;
        private readonly ITrainingService _trainings;
        private readonly IResourceService _resources;
        private readonly IBenefitService _benefits;
        private readonly IReportService _reports;

        public ProgramManagerController(IProgramService programs, ITrainingService trainings,
            IResourceService resources, IBenefitService benefits, IReportService reports)
        {
            _programs = programs; _trainings = trainings; _resources = resources;
            _benefits = benefits; _reports = reports;
        }

        private int? UserId => HttpContext.Session.GetInt32("UserId");

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var model = await _programs.GetDashboardAsync(UserId.Value);
            return View(model);
        }

        [Route("ProgramManagement")]
        public async Task<IActionResult> ProgramManagement()
        {
            var programs = await _programs.GetAllAsync();
            return View(programs);
        }

        [HttpGet, Route("CreateProgram")]
        public IActionResult CreateProgram() => View();

        [HttpPost, Route("CreateProgram")]
        public async Task<IActionResult> CreateProgram(EmploymentProgram model)
        {
            var (success, msg) = await _programs.CreateAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ProgramManagement");
        }

        [HttpGet, Route("EditProgram/{id}")]
        public async Task<IActionResult> EditProgram(int id)
        {
            var program = await _programs.GetByIdAsync(id);
            return program == null ? NotFound() : View(program);
        }

        [HttpPost, Route("EditProgram/{id}")]
        public async Task<IActionResult> EditProgram(int id, EmploymentProgram model)
        {
            model.Id = id;
            var (success, msg) = await _programs.UpdateAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ProgramManagement");
        }

        [HttpPost, Route("DeleteProgram/{id}")]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            var (success, msg) = await _programs.DeleteAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ProgramManagement");
        }

        [Route("BudgetMonitoring")]
        public async Task<IActionResult> BudgetMonitoring()
        {
            var programs = await _programs.GetAllAsync();
            return View(programs);
        }

        [Route("PerformanceTracking")]
        public async Task<IActionResult> PerformanceTracking()
        {
            var model = await _programs.GetDashboardAsync(UserId ?? 0);
            return View(model);
        }

        [Route("TrainingManagement")]
        public async Task<IActionResult> TrainingManagement()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var trainings = await _trainings.GetAllAsync();
            return View(trainings);
        }

        [HttpGet, Route("CreateTraining")]
        public async Task<IActionResult> CreateTraining()
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            ViewBag.Programs = await _programs.GetAllAsync();
            return View();
        }

        [HttpPost, Route("CreateTraining")]
        public async Task<IActionResult> CreateTraining(Training model)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _trainings.CreateAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            if (success) return RedirectToAction("TrainingManagement");
            ViewBag.Programs = await _programs.GetAllAsync();
            return View(model);
        }

        [HttpGet, Route("EditTraining/{id}")]
        public async Task<IActionResult> EditTraining(int id)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var training = await _trainings.GetByIdAsync(id);
            if (training == null) return NotFound();
            ViewBag.Programs = await _programs.GetAllAsync();
            return View(training);
        }

        [HttpPost, Route("EditTraining/{id}")]
        public async Task<IActionResult> EditTraining(int id, Training model)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            model.Id = id;
            var (success, msg) = await _trainings.UpdateAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            if (success) return RedirectToAction("TrainingManagement");
            ViewBag.Programs = await _programs.GetAllAsync();
            return View(model);
        }

        [HttpPost, Route("DeleteTraining/{id}")]
        public async Task<IActionResult> DeleteTraining(int id)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var (success, msg) = await _trainings.DeleteAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("TrainingManagement");
        }

        [Route("ResourceManagement")]
        public async Task<IActionResult> ResourceManagement()
        {
            var programs = await _programs.GetAllAsync();
            return View(programs);
        }

        [HttpPost, Route("CreateResource")]
        public async Task<IActionResult> CreateResource(Resource model)
        {
            var (success, msg) = await _resources.CreateAsync(model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("ResourceManagement");
        }

        [Route("Reports")]
        public async Task<IActionResult> Reports()
        {
            var reports = await _reports.GetAllAsync();
            return View(reports);
        }

        [HttpPost, Route("GenerateReport")]
        public async Task<IActionResult> GenerateReport(string reportName, string reportType)
        {
            if (UserId == null) return RedirectToAction("Login", "Account");
            var report = new Report { ReportName = reportName, ReportType = reportType, GeneratedBy = UserId.Value, GeneratedDate = DateTime.Now };
            var (success, msg) = await _reports.GenerateAsync(report);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("Reports");
        }
    }
}
