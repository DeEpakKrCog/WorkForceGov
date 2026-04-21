using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using WorkForceGovProject.Authentication;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Repositories.Citizen;
using WorkForceGovProject.Repositories.Common;
using WorkForceGovProject.Repositories.Employer;
using WorkForceGovProject.Repositories.LaborOfficer;
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.Employer;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication ──────────────────────────────────────────────────────
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
    .AddScheme<AuthenticationSchemeOptions, XUserIdAuthenticationHandler>("XUserId", options => { });
builder.Services.AddAuthorization();

// ── Controllers ─────────────────────────────────────────────────────────────
builder.Services.AddControllers().AddJsonOptions(o => {
    o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// ── Swagger (no OpenApi model usage to avoid package conflicts) ──────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new()
    {
        Title = "WorkForceGov — Employer API",
        Version = "v1",
        Description = "Step 1: POST /api/auth/login with email+password to get a JWT token.<br/>" +
                      "Step 2: Click Authorize and enter:  Bearer {your token}"
    });

    // Note: intentionally not adding the Swagger 'Authorize' input here to avoid
    // direct Microsoft.OpenApi model references which caused package resolution errors.
    c.EnableAnnotations();
});

// ── Dependency Injection — Repositories ────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
builder.Services.AddScoped<IEmployerDocumentRepository, EmployerDocumentRepository>();
builder.Services.AddScoped<IJobOpeningRepository, JobOpeningRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// ── Dependency Injection — Services ───────────────────────────────────────
builder.Services.AddScoped<IAccountService, AccountService>();   // needed for AuthController login
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmployerService, EmployerService>();
builder.Services.AddScoped<IJobService, JobService>();

builder.Services.AddCors(o =>
    o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Apply any pending EF Core migrations at startup so the DB schema matches the model.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    try
    {
        // Ensure missing Employer columns exist (safe if migrations not applied or migrations are in another assembly)
        var ensureColumnsSql = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE [object_id]=OBJECT_ID(N'[dbo].[Employers]') AND [name]='Description')
    ALTER TABLE [dbo].[Employers] ADD [Description] nvarchar(1000) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE [object_id]=OBJECT_ID(N'[dbo].[Employers]') AND [name]='PhoneNumber')
    ALTER TABLE [dbo].[Employers] ADD [PhoneNumber] nvarchar(20) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE [object_id]=OBJECT_ID(N'[dbo].[Employers]') AND [name]='Website')
    ALTER TABLE [dbo].[Employers] ADD [Website] nvarchar(200) NULL;";

        db.Database.ExecuteSqlRaw(ensureColumnsSql);
    }
    catch (Exception ex)
    {
        // If automatic alteration fails, continue startup but surface the error in logs
        Console.WriteLine($"Warning: failed to ensure Employer columns exist: {ex.Message}");
    }
}

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employer API v1");
    c.RoutePrefix = string.Empty;
    c.DocumentTitle = "WorkForceGov — Employer API";
    c.DisplayRequestDuration();
});

app.UseHttpsRedirection();
app.UseAuthentication();   // ← must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.Run();
