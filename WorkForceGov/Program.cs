using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;

// ── Repository Interfaces ──
using WorkForceGovProject.Interfaces.Repositories;

// ── Service Interfaces ──
using WorkForceGovProject.Interfaces.Services;

// ── Repository Implementations ──
using WorkForceGovProject.Repositories.Common;
using WorkForceGovProject.Repositories.Citizen;
using WorkForceGovProject.Repositories.Employer;
using WorkForceGovProject.Repositories.LaborOfficer;
using WorkForceGovProject.Repositories.GovernmentAuditor;

// ── Service Implementations ──
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.Citizen;
using WorkForceGovProject.Services.Employer;
using WorkForceGovProject.Services.LaborOfficer;
using WorkForceGovProject.Services.ComplianceOfficer;
using WorkForceGovProject.Services.GovernmentAuditor;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════
//  DATABASE
// ═══════════════════════════════════════════════════════
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ═══════════════════════════════════════════════════════
//  MVC + API CONTROLLERS
// ═══════════════════════════════════════════════════════
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

// ═══════════════════════════════════════════════════════
//  DEPENDENCY INJECTION — REPOSITORIES
//  Each layer only communicates via defined interfaces.
// ═══════════════════════════════════════════════════════

// Generic base
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Common module
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddScoped<IProgramRepository, ProgramRepository>();
builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();

// Citizen module
builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
builder.Services.AddScoped<ICitizenDocumentRepository, CitizenDocumentRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IBenefitRepository, BenefitRepository>();

// Employer module
builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
builder.Services.AddScoped<IEmployerDocumentRepository, EmployerDocumentRepository>();
builder.Services.AddScoped<IJobOpeningRepository, JobOpeningRepository>();

// Labor Officer module
builder.Services.AddScoped<IComplianceRecordRepository, ComplianceRecordRepository>();
builder.Services.AddScoped<IViolationRepository, ViolationRepository>();
builder.Services.AddScoped<INotificationRepository, WorkForceGovProject.Repositories.LaborOfficer.NotificationRepository>();

// Government Auditor module
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// ═══════════════════════════════════════════════════════
//  DEPENDENCY INJECTION — SERVICES
//  Services depend ONLY on repository interfaces — never
//  on other service implementations directly.
// ═══════════════════════════════════════════════════════

// Common services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IProgramService, ProgramService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IResourceService, ResourceService>();

// Citizen module services
builder.Services.AddScoped<ICitizenService, CitizenService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IBenefitService, BenefitService>();

// Employer module service
builder.Services.AddScoped<IEmployerService, EmployerService>();
builder.Services.AddScoped<IJobService, JobService>();

// Labor Officer module service
builder.Services.AddScoped<ILaborOfficerService, LaborOfficerService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();

// Compliance Officer module service (ACTIVE — no longer read-only)
builder.Services.AddScoped<IComplianceOfficerService, ComplianceOfficerService>();

// Government Auditor module service (ACTIVE — no longer read-only)
builder.Services.AddScoped<IGovernmentAuditorService, GovernmentAuditorService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ═══════════════════════════════════════════════════════
//  SESSION
// ═══════════════════════════════════════════════════════
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Ensure DB is created with seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// MVC route for Razor views
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// API routes (Postman-ready — all under /api/*)
app.MapControllers();

app.Run();
