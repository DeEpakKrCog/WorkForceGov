using Microsoft.AspNetCore.Mvc;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;
using WorkForceGovProject.Interfaces.Services;

namespace WorkForceGovProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly IJobService _jobs;
        public HomeController(IJobService jobs) { _jobs = jobs; }

        public async Task<IActionResult> Index()
        {
            ViewBag.RecentJobs = (await _jobs.GetOpenJobsAsync()).Take(6).ToList();
            return View();
        }

        public IActionResult Privacy() => View();
    }

    public class AccountController : Controller
    {
        private readonly IAccountService _account;
        private readonly ICitizenService _citizen;
        private readonly ISystemLogService _logs;

        public AccountController(IAccountService account, ICitizenService citizen, ISystemLogService logs)
        {
            _account = account;
            _citizen = citizen;
            _logs = logs;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var (success, message, user) = await _account.LoginAsync(model.Email, model.Password);
            if (!success || user == null)
            {
                TempData["ErrorMessage"] = message;
                return View(model);
            }

            string NormalizeRole(string r)
            {
                if (string.IsNullOrWhiteSpace(r)) return string.Empty;
                var cleaned = r.Replace(" ", "").Replace("-", "").ToLower();
                return cleaned switch
                {
                    "citizen" => "Citizen",
                    "employer" => "Employer",
                    "systemadmin" or "admin" or "administrator" => "SystemAdmin",
                    "labourofficer" or "laborofficer" => "LaborOfficer",
                    "complianceofficer" or "compliance" => "ComplianceOfficer",
                    "governmentauditor" => "GovernmentAuditor",
                    "programmanager" => "ProgramManager",
                    _ => r
                };
            }

            var canonicalRole = NormalizeRole(user.Role);
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", canonicalRole);
            HttpContext.Session.SetString("UserEmail", user.Email);

            // Log the login event
            try { await _logs.LogAsync(user.Id, "UserLogin", canonicalRole, HttpContext.Connection.RemoteIpAddress?.ToString()); }
            catch { /* non-fatal */ }

            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

            return canonicalRole switch
            {
                "Citizen" => RedirectToAction("Dashboard", "Citizen"),
                "Employer" => RedirectToAction("Dashboard", "Employer"),
                "SystemAdmin" => RedirectToAction("Dashboard", "Admin"),
                "LaborOfficer" => RedirectToAction("Dashboard", "LaborOfficer"),
                "ComplianceOfficer" => RedirectToAction("Dashboard", "ComplianceOfficer"),
                "GovernmentAuditor" => RedirectToAction("Dashboard", "GovernmentAuditor"),
                "ProgramManager" => RedirectToAction("Dashboard", "ProgramManager"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var (success, message) = await _account.RegisterAsync(model);
            if (success)
            {
                // Auto-create citizen profile
                if (model.Role == "Citizen")
                {
                    try
                    {
                        var user = await _account.GetByIdAsync(0);
                        // Find the newly created user by email
                        var allUsers = await _account.GetAllUsersAsync();
                        var newUser = allUsers.FirstOrDefault(u => u.Email == model.Email);
                        if (newUser != null)
                        {
                            await _citizen.CreateProfileAsync(newUser.Id, model.FullName, model.Email);
                            await _logs.LogAsync(newUser.Id, "UserRegistered", "Citizen");
                        }
                    }
                    catch { /* non-fatal */ }
                }
                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            TempData["ErrorMessage"] = message;
            return View(model);
        }

        public IActionResult Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                try { _ = _logs.LogAsync(userId.Value, "UserLogout", null); }
                catch { }
            }
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login");
        }
    }
}
