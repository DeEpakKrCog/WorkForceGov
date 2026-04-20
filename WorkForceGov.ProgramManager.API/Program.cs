using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Repositories.Common;
using WorkForceGovProject.Repositories.LaborOfficer;
using WorkForceGovProject.Repositories.ProgramManager;
using WorkForceGovProject.Services.Common;
using WorkForceGovProject.Services.ProgramManager;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers().AddJsonOptions(o => {
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new() { Title = "WorkForceGov — Program Manager API", Version = "v1",
        Description = "Program Manager microservice: benefit approval workflow, program management, training oversight & budget tracking." });
    //c.AddSecurityDefinition("UserIdHeader", new() {
    //    Name = "X-User-Id", Type = (object)0, In = (object)0, Description = "Program Manager User ID. Example: 7" });
    //c.AddSecurityRequirement(new() {{ new() {
    //    Reference = new() { Type = (object)0, Id = "UserIdHeader" }}, Array.Empty<string>() }});
    c.EnableAnnotations();
});
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddScoped<IProgramRepository, ProgramRepository>();
builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
builder.Services.AddScoped<ITrainingEnrollmentRepository, TrainingEnrollmentRepository>();
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
//builder.Services.AddScoped<IBenefitRepository, BenefitRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ProgramManagerRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProgramService, ProgramService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IProgramManagerService, ProgramManagerService>();
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
var app = builder.Build();
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Program Manager API v1"); c.RoutePrefix = string.Empty; c.DocumentTitle = "WorkForceGov — Program Manager API"; });
app.UseHttpsRedirection(); app.UseAuthorization(); app.MapControllers(); app.Run();
