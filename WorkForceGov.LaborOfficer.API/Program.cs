using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using WorkForceGovProject.Authentication;
using WorkForceGovProject.Data;
using Microsoft.Extensions.DependencyInjection;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Repositories.Common;
using WorkForceGovProject.Repositories.Citizen;
using WorkForceGovProject.Repositories.Employer;
using WorkForceGovProject.Repositories.LaborOfficer;
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.LaborOfficer;
using WorkForceGovProject.Services.ComplianceOfficer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Authentication ─────────────────────────────────────────────────────────
// Try JWT first (from Bearer token), then fall back to X-User-Id header
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

// ── Controllers ────────────────────────────────────────────────────────────
builder.Services.AddControllers().AddJsonOptions(o => {
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new() { Title = "WorkForceGov — Labor Officer API", Version = "v1",
        Description = "Labor Officer microservice: citizen document verification, complaint investigation, compliance records & violation tracking." });
    c.EnableAnnotations();
});
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddScoped<ICitizenDocumentRepository, CitizenDocumentRepository>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IComplianceRecordRepository, ComplianceRecordRepository>();
builder.Services.AddScoped<IViolationRepository, ViolationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
builder.Services.AddScoped<IEmployerDocumentRepository, EmployerDocumentRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ILaborOfficerService, LaborOfficerService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<IComplianceOfficerService, ComplianceOfficerService>();
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
var app = builder.Build();
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Labor Officer API v1"); c.RoutePrefix = string.Empty; c.DocumentTitle = "WorkForceGov — Labor Officer API"; c.DefaultModelsExpandDepth(-1); });
app.UseHttpsRedirection();
app.UseAuthentication();   // ← must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

// Ensure database schema is up-to-date on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();

