namespace WorkForceGovProject.Models.ViewModels
{
    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Citizen";
        public string? Phone { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalCitizens { get; set; }
        public int TotalEmployers { get; set; }
        public int TotalJobs { get; set; }
        public int OpenJobs { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int TotalPrograms { get; set; }
        public int ActivePrograms { get; set; }
        public int PendingDocuments { get; set; }
        public int ComplianceAlerts { get; set; }
        public int TotalAudits { get; set; }
        public int OpenAudits { get; set; }
        public int TotalReports { get; set; }
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int TotalViolations { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalBenefitsPaid { get; set; }
        public List<Notification> RecentNotifications { get; set; } = new();
        public List<SystemLog> RecentLogs { get; set; } = new();
        public List<JobOpening> RecentJobs { get; set; } = new();
        public List<Application> RecentApplications { get; set; } = new();
        public List<EmploymentProgram> Programs { get; set; } = new();
        public List<ComplianceRecord> RecentCompliance { get; set; } = new();
        public List<Audit> RecentAudits { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalComplianceRecords { get; set; }
    }

    public class CitizenDashboardViewModel
    {
        public Citizen Citizen { get; set; } = null!;
        public int ActiveApplications { get; set; }
        public int TotalBenefits { get; set; }
        public decimal TotalBenefitAmount { get; set; }
        public int DocumentCount { get; set; }
        public int PendingDocs { get; set; }
        public int VerifiedDocs { get; set; }
        public List<Application> RecentApplications { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public List<JobOpening> RecommendedJobs { get; set; } = new();
    }

    public class EmployerDashboardViewModel
    {
        public Employer Employer { get; set; } = null!;
        public int TotalJobPostings { get; set; }
        public int OpenJobPostings { get; set; }
        public int TotalApplicationsReceived { get; set; }
        public int PendingApplications { get; set; }
        public int ShortlistedCandidates { get; set; }
        public int HiredCandidates { get; set; }
        public List<JobOpening> RecentJobs { get; set; } = new();
        public List<Application> RecentApplications { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }

    public class JobSearchViewModel
    {
        public string? Location { get; set; }
        public string? Category { get; set; }
        public string? Keyword { get; set; }
        public List<JobOpening> Jobs { get; set; } = new();
    }

    public class UserManageViewModel
    {
        public List<User> Users { get; set; } = new();
    }

    public class CreateUserViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Citizen";
        public string? Phone { get; set; }
    }

    public class ComplianceDashboardViewModel
    {
        public int PendingReviews { get; set; }
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int TotalViolations { get; set; }
        public int TotalInspections { get; set; }
        public List<Employer> Employers { get; set; } = new();
        public List<Complaint> RecentComplaints { get; set; } = new();
        public List<Violation> RecentViolations { get; set; } = new();
        public List<ComplianceRecord> RecentRecords { get; set; } = new();
    }

    public class AuditorDashboardViewModel
    {
        public int TotalAudits { get; set; }
        public int OpenAudits { get; set; }
        public int CompletedAudits { get; set; }
        public int TotalCompliance { get; set; }
        public int NonCompliant { get; set; }
        public int TotalPrograms { get; set; }
        public List<Audit> RecentAudits { get; set; } = new();
        public List<ComplianceRecord> RecentCompliance { get; set; } = new();
        public List<EmploymentProgram> Programs { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }

    public class LaborOfficerDashboardViewModel
    {
        public int PendingDocuments { get; set; }
        public int ComplianceAlerts { get; set; }
        public int PendingApplications { get; set; }
        public int ApprovedApplications { get; set; }
        public int RejectedApplications { get; set; }
        public List<CitizenDocument> PendingDocs { get; set; } = new();
        public List<Application> RecentApplications { get; set; } = new();
        public List<Employer> FlaggedEmployers { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }

    public class ProgramManagerDashboardViewModel
    {
        public int TotalPrograms { get; set; }
        public int ActivePrograms { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal BudgetUtilized { get; set; }
        public int TotalTrainings { get; set; }
        public int TotalBeneficiaries { get; set; }
        public List<EmploymentProgram> Programs { get; set; } = new();
        public List<Training> ActiveTrainings { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
