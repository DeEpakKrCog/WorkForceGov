using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;
// Interfaces
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
// Implementations (These folders contain the actual .cs classes)
using WorkForceGovProject.Repositories.Common;
using WorkForceGovProject.Repositories.Citizen;
using WorkForceGovProject.Repositories.Employer;
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.Citizen;
using WorkForceGovProject.Services.Employer;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Controllers & JSON formatting (prevents loops in API data)
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Dependency Injection — REPOSITORIES
// Generic base
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Common module
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddScoped<IProgramRepository, ProgramRepository>();
builder.Services.AddScoped<ITrainingRepository, TrainingRepository>(); // FIXED: Changed implementation to TrainingRepository
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();

// Citizen module
builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
builder.Services.AddScoped<ICitizenDocumentRepository, CitizenDocumentRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IBenefitRepository, BenefitRepository>();

// Employer module
builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
builder.Services.AddScoped<IJobOpeningRepository, JobOpeningRepository>();

// Labor Officer module
builder.Services.AddScoped<INotificationRepository, WorkForceGovProject.Repositories.LaborOfficer.NotificationRepository>();

// 4. Dependency Injection — SERVICES
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<IProgramService, ProgramService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<ICitizenService, CitizenService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IBenefitService, BenefitService>();
builder.Services.AddScoped<IEmployerService, EmployerService>();
builder.Services.AddScoped<IJobService, JobService>();

var app = builder.Build(); // The app should now build without the 'Cannot instantiate' error

// 5. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();