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
using WorkForceGovProject.Repositories.LaborOfficer;
using WorkForceGovProject.Services.Citizen;
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.Employer;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WorkForceGov Citizen API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithProperty("Application", "Citizen.API"));

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
            Title = "WorkForceGov — Citizen API",
            Version = "v1",
            Description = "Citizen Services - Job Search, Applications, Benefits, Trainings, Documents, Notifications."
        });
        c.EnableAnnotations();
    });

    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
    builder.Services.AddScoped<IProgramRepository, ProgramRepository>();
    builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
    builder.Services.AddScoped<ITrainingEnrollmentRepository, TrainingEnrollmentRepository>();
    builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
    builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
    builder.Services.AddScoped<ICitizenDocumentRepository, CitizenDocumentRepository>();
    builder.Services.AddScoped<IEmployerDocumentRepository, EmployerDocumentRepository>();
    builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
    builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
    builder.Services.AddScoped<IBenefitRepository, BenefitRepository>();
    builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
    builder.Services.AddScoped<IJobOpeningRepository, JobOpeningRepository>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IProgramService, ProgramService>();
    builder.Services.AddScoped<ITrainingService, TrainingService>();
    builder.Services.AddScoped<ICitizenService, CitizenService>();
    builder.Services.AddScoped<IDocumentService, DocumentService>();
    builder.Services.AddScoped<IApplicationService, ApplicationService>();
    builder.Services.AddScoped<IBenefitService, BenefitService>();
    builder.Services.AddScoped<IJobService, JobService>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Citizen API v1");
        c.RoutePrefix = string.Empty;
        c.DocumentTitle = "WorkForceGov — Citizen API";
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
    Log.Fatal(ex, "Citizen API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
