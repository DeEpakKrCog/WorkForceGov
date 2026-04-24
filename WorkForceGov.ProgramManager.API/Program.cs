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
using WorkForceGovProject.Repositories.Citizen;
using WorkForceGovProject.Repositories.Common;
using WorkForceGovProject.Repositories.Employer;
using WorkForceGovProject.Repositories.GovernmentAuditor;
using WorkForceGovProject.Repositories.LaborOfficer;
using WorkForceGovProject.Repositories.ProgramManager;
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.ProgramManager;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WorkForceGov Program Manager API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithProperty("Application", "ProgramManager.API"));

    builder.Services.AddDbContext<ApplicationDbContext>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

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

    builder.Services.AddControllers().AddJsonOptions(o => {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => {
        c.SwaggerDoc("v1", new()
        {
            Title = "WorkForceGov — Program Manager API",
            Version = "v1",
            Description = "Program Manager microservice: manage employment programs, trainings, resources, track performance & citizen enrollments."
        });
        c.EnableAnnotations();
    });

    // Generic repo
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    // Common repos
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
    builder.Services.AddScoped<IProgramRepository, ProgramRepository>();
    builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
    builder.Services.AddScoped<ITrainingEnrollmentRepository, TrainingEnrollmentRepository>();
    builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
    // Citizen repos
    builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
    builder.Services.AddScoped<IBenefitRepository, BenefitRepository>();
    builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
    builder.Services.AddScoped<ICitizenDocumentRepository, CitizenDocumentRepository>();
    builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
    // Employer repos
    builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
    builder.Services.AddScoped<IJobOpeningRepository, JobOpeningRepository>();
    // Audit repos
    builder.Services.AddScoped<IAuditRepository, AuditRepository>();
    builder.Services.AddScoped<IReportRepository, ReportRepository>();
    // Notification repos
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    // ProgramManager-specific
    builder.Services.AddScoped<ProgramManagerRepository>();

    // Services
    builder.Services.AddScoped<WorkForceGovProject.Interfaces.Services.IAuthenticationService,
                                WorkForceGovProject.Services.Common.AuthenticationService>();
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<ISystemLogService, SystemLogService>();
    builder.Services.AddScoped<IProgramManagerService, ProgramManagerService>();
    builder.Services.AddScoped<ITrainingService, TrainingService>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Program Manager API v1");
        c.RoutePrefix = string.Empty;
        c.DocumentTitle = "WorkForceGov — Program Manager API";
        c.DefaultModelsExpandDepth(-1);
    });

    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Program Manager API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
