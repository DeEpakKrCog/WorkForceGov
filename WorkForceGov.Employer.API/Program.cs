using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
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
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.Employer;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WorkForceGov Employer API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithProperty("Application", "Employer.API"));

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
            Title = "WorkForceGov — Employer API",
            Version = "v1",
            Description = "Employer microservice: job postings, applications, employer profile & documents."
        });
        c.EnableAnnotations();
    });

    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
    builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
    builder.Services.AddScoped<IEmployerDocumentRepository, EmployerDocumentRepository>();
    builder.Services.AddScoped<IJobOpeningRepository, JobOpeningRepository>();
    builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IEmployerService, EmployerService>();
    builder.Services.AddScoped<IJobService, JobService>();

    builder.Services.AddCors(o =>
        o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    var app = builder.Build();

    // Apply EF Core migrations at startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        try
        {
            var sql = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE [object_id]=OBJECT_ID(N'[dbo].[Employers]') AND [name]='Description')
    ALTER TABLE [dbo].[Employers] ADD [Description] nvarchar(1000) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE [object_id]=OBJECT_ID(N'[dbo].[Employers]') AND [name]='PhoneNumber')
    ALTER TABLE [dbo].[Employers] ADD [PhoneNumber] nvarchar(20) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE [object_id]=OBJECT_ID(N'[dbo].[Employers]') AND [name]='Website')
    ALTER TABLE [dbo].[Employers] ADD [Website] nvarchar(200) NULL;";
            db.Database.ExecuteSqlRaw(sql);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Warning: failed to ensure Employer columns exist");
        }
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseCors("AllowAll");
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employer API v1");
        c.RoutePrefix = string.Empty;
        c.DocumentTitle = "WorkForceGov — Employer API";
        c.DisplayRequestDuration();
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
    Log.Fatal(ex, "Employer API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
