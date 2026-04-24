using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using WorkForceGovProject.Authentication;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Middleware;
using WorkForceGovProject.Repositories.Admin;
using WorkForceGovProject.Repositories.Citizen;
using WorkForceGovProject.Repositories.Common;
using WorkForceGovProject.Repositories.Employer;
using WorkForceGovProject.Repositories.GovernmentAuditor;
using WorkForceGovProject.Repositories.LaborOfficer;
using WorkForceGovProject.Services.Admin;
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.GovernmentAuditor;

// ── Serilog — bootstrap logger (captures startup errors) ────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WorkForceGov Admin API");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog — read full config from appsettings.json ────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithProperty("Application", "Admin.API"));

    // ── Database ─────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<ApplicationDbContext>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ── Global Exception Handler ─────────────────────────────────────────────
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ── Authentication ───────────────────────────────────────────────────────
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

    builder.Services
        .AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddScheme<AuthenticationSchemeOptions, XUserIdAuthenticationHandler>("XUserId", _ => { });

    builder.Services.AddAuthorization();

    // ── Controllers ──────────────────────────────────────────────────────────
    builder.Services.AddControllers().AddJsonOptions(o => {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => {
        c.SwaggerDoc("v1", new()
        {
            Title = "WorkForceGov — Admin API",
            Version = "v1",
            Description = "Admin microservice: user management, employer oversight, notifications, reports & monitoring."
        });
        c.EnableAnnotations();
    });

    // ── Repositories ─────────────────────────────────────────────────────────
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
    builder.Services.AddScoped<IProgramRepository, ProgramRepository>();
    builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
    builder.Services.AddScoped<ITrainingEnrollmentRepository, TrainingEnrollmentRepository>();
    builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
    builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
    builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
    builder.Services.AddScoped<IBenefitRepository, BenefitRepository>();
    builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
    builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
    builder.Services.AddScoped<IJobOpeningRepository, JobOpeningRepository>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<IAuditRepository, AuditRepository>();
    builder.Services.AddScoped<IReportRepository, ReportRepository>();
    builder.Services.AddScoped<AdminRepository>();

    // ── Services ─────────────────────────────────────────────────────────────
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<ISystemLogService, SystemLogService>();
    builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IAdminService, AdminService>();

    builder.Services.AddCors(o =>
        o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseCors("AllowAll");
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin API v1");
        c.RoutePrefix = string.Empty;
        c.DocumentTitle = "WorkForceGov — Admin API";
        c.DefaultModelsExpandDepth(-1);
    });

    app.UseExceptionHandler();   // ← GlobalExceptionHandler kicks in here
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Admin API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
