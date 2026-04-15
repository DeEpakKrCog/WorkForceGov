using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Citizen> Citizens { get; set; }
        public DbSet<CitizenDocument> CitizenDocuments { get; set; } 
        public DbSet<Employer> Employers { get; set; }
        public DbSet<EmployerDocument> EmployerDocuments { get; set; }
        public DbSet<JobOpening> JobOpenings { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Benefit> Benefits { get; set; }
        public DbSet<EmploymentProgram> EmploymentPrograms { get; set; }
        public DbSet<Training> Trainings { get; set; }
        public DbSet<TrainingEnrollment> TrainingEnrollments { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ComplianceRecord> ComplianceRecords { get; set; }
        public DbSet<Audit> Audits { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Violation> Violations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User -> Citizen (one-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Citizen)
                .WithOne(c => c.User)
                .HasForeignKey<Citizen>(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Employer (one-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employer)
                .WithOne(e => e.User)
                .HasForeignKey<Employer>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Citizen -> Documents
            modelBuilder.Entity<CitizenDocument>()
                .HasOne(d => d.Citizen)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CitizenId)
                .OnDelete(DeleteBehavior.Cascade);

            // Citizen -> Applications
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Citizen)
                .WithMany(c => c.Applications)
                .HasForeignKey(a => a.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            // JobOpening -> Applications
            modelBuilder.Entity<Application>()
                .HasOne(a => a.JobOpening)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobOpeningId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employer -> JobOpenings
            modelBuilder.Entity<JobOpening>()
                .HasOne(j => j.Employer)
                .WithMany(e => e.JobOpenings)
                .HasForeignKey(j => j.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Employer -> EmployerDocuments
            modelBuilder.Entity<EmployerDocument>()
                .HasOne(d => d.Employer)
                .WithMany(e => e.Documents)
                .HasForeignKey(d => d.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);

            // EmploymentProgram -> Training
            modelBuilder.Entity<Training>()
                .HasOne(t => t.Program)
                .WithMany(p => p.Trainings)
                .HasForeignKey(t => t.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            // Training -> TrainingEnrollments
            modelBuilder.Entity<TrainingEnrollment>()
                .HasOne(e => e.Training)
                .WithMany(t => t.Enrollments)
                .HasForeignKey(e => e.TrainingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Citizen -> TrainingEnrollments
            modelBuilder.Entity<TrainingEnrollment>()
                .HasOne(e => e.Citizen)
                .WithMany(c => c.TrainingEnrollments)
                .HasForeignKey(e => e.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            // EmploymentProgram -> Resources
            modelBuilder.Entity<Resource>()
                .HasOne(r => r.Program)
                .WithMany(p => p.Resources)
                .HasForeignKey(r => r.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            // EmploymentProgram -> Benefits
            modelBuilder.Entity<Benefit>()
                .HasOne(b => b.Program)
                .WithMany(p => p.Benefits)
                .HasForeignKey(b => b.ProgramId)
                .OnDelete(DeleteBehavior.Restrict);

            // Citizen -> Benefits
            modelBuilder.Entity<Benefit>()
                .HasOne(b => b.Citizen)
                .WithMany(c => c.Benefits)
                .HasForeignKey(b => b.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification -> User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SystemLog -> User
            modelBuilder.Entity<SystemLog>()
                .HasOne(s => s.User)
                .WithMany(u => u.SystemLogs)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Audit -> User (Officer)
            modelBuilder.Entity<Audit>()
                .HasOne(a => a.Officer)
                .WithMany(u => u.Audits)
                .HasForeignKey(a => a.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Report -> User
            modelBuilder.Entity<Report>()
                .HasOne(r => r.GeneratedByUser)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.GeneratedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Complaint -> User
            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Complaint -> Employer
            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Employer)
                .WithMany()
                .HasForeignKey(c => c.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Violation -> Employer
            modelBuilder.Entity<Violation>()
                .HasOne(v => v.Employer)
                .WithMany()
                .HasForeignKey(v => v.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Violation -> Officer
            modelBuilder.Entity<Violation>()
                .HasOne(v => v.Officer)
                .WithMany()
                .HasForeignKey(v => v.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ComplianceRecord -> Officer
            modelBuilder.Entity<ComplianceRecord>()
                .HasOne(c => c.Officer)
                .WithMany()
                .HasForeignKey(c => c.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            // CitizenDocument -> VerifiedBy
            modelBuilder.Entity<CitizenDocument>()
                .HasOne(d => d.VerifiedBy)
                .WithMany()
                .HasForeignKey(d => d.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique index on User Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Seed default data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "John Citizen", Email = "john.citizen@gov.local", Password = "password123", Role = "Citizen", Status = "Active", CreatedAt = new DateTime(2026, 1, 1) },
                new User { Id = 2, FullName = "Jane Employer", Email = "jane.employer@company.com", Password = "password123", Role = "Employer", Status = "Active", CreatedAt = new DateTime(2026, 1, 1) },
                new User { Id = 3, FullName = "Officer Smith", Email = "officer.smith@gov.local", Password = "password123", Role = "LaborOfficer", Status = "Active", CreatedAt = new DateTime(2026, 1, 1) },
                new User { Id = 4, FullName = "Admin User", Email = "admin@gov.local", Password = "password123", Role = "SystemAdmin", Status = "Active", CreatedAt = new DateTime(2026, 1, 1) },
                new User { Id = 5, FullName = "Manager Davis", Email = "davis.manager@gov.local", Password = "password123", Role = "ProgramManager", Status = "Active", CreatedAt = new DateTime(2026, 1, 1) },
                new User { Id = 6, FullName = "Officer Compliance", Email = "compliance@gov.local", Password = "password123", Role = "ComplianceOfficer", Status = "Active", CreatedAt = new DateTime(2026, 1, 1) },
                new User { Id = 7, FullName = "Auditor Brown", Email = "auditor@gov.local", Password = "password123", Role = "GovernmentAuditor", Status = "Active", CreatedAt = new DateTime(2026, 1, 1) }
            );

            modelBuilder.Entity<Citizen>().HasData(
                new Citizen { Id = 1, UserId = 1, FullName = "John Citizen", Email = "john.citizen@gov.local", DOB = new DateTime(1990, 5, 15), Gender = "Male", Address = "123 Main Street, City, State 12345", PhoneNumber = "+1-555-0101", DocumentStatus = "Verified" }
            );

            modelBuilder.Entity<Employer>().HasData(
                new Employer { Id = 1, UserId = 2, CompanyName = "TechCorp Industries", Industry = "Technology", Address = "456 Business Ave, City", ContactInfo = "jane.employer@company.com", Status = "Verified", RegistrationDate = new DateTime(2026, 1, 1) }
            );

            modelBuilder.Entity<JobOpening>().HasData(
                new JobOpening { Id = 1, EmployerId = 1, JobTitle = "Software Developer", Description = "Seeking experienced .NET developers for enterprise applications", Location = "New York, NY", JobCategory = "Technology", SalaryMin = 80000, SalaryMax = 120000, PostedDate = new DateTime(2026, 3, 1), Status = "Open" },
                new JobOpening { Id = 2, EmployerId = 1, JobTitle = "Project Manager", Description = "Looking for project management professionals with Agile experience", Location = "Chicago, IL", JobCategory = "Management", SalaryMin = 70000, SalaryMax = 100000, PostedDate = new DateTime(2026, 3, 5), Status = "Open" },
                new JobOpening { Id = 3, EmployerId = 1, JobTitle = "Data Analyst", Description = "Data analysis and reporting role with Python/SQL skills", Location = "Remote", JobCategory = "Analytics", SalaryMin = 60000, SalaryMax = 90000, PostedDate = new DateTime(2026, 3, 10), Status = "Open" }
            );

            modelBuilder.Entity<EmploymentProgram>().HasData(
                new EmploymentProgram { Id = 1, ProgramName = "Tech Skills Initiative", Description = "Technology training and certification program", ProgramType = "Training", TotalBudget = 500000, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31), Status = "Active" },
                new EmploymentProgram { Id = 2, ProgramName = "Healthcare Career Path", Description = "Medical assistant and healthcare training program", ProgramType = "Training", TotalBudget = 350000, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 11, 30), Status = "Active" },
                new EmploymentProgram { Id = 3, ProgramName = "Small Business Support", Description = "Entrepreneurship and small business development", ProgramType = "Subsidy", TotalBudget = 200000, StartDate = new DateTime(2026, 4, 1), EndDate = new DateTime(2026, 10, 31), Status = "Active" }
            );

            modelBuilder.Entity<Training>().HasData(
                new Training { Id = 1, ProgramId = 1, Title = "Introduction to .NET Development", Description = "Hands-on .NET and C# fundamentals for beginners", StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 6, 30), Status = "Active" },
                new Training { Id = 2, ProgramId = 1, Title = "Cloud Computing with Azure", Description = "Microsoft Azure fundamentals and cloud deployment", StartDate = new DateTime(2026, 6, 1), EndDate = new DateTime(2026, 7, 31), Status = "Active" },
                new Training { Id = 3, ProgramId = 2, Title = "Medical Assistant Certification", Description = "Certified medical assistant preparation course", StartDate = new DateTime(2026, 5, 15), EndDate = new DateTime(2026, 8, 15), Status = "Active" },
                new Training { Id = 4, ProgramId = 3, Title = "Business Plan Development", Description = "Create and present a fundable business plan", StartDate = new DateTime(2026, 4, 15), EndDate = new DateTime(2026, 5, 30), Status = "Active" }
            );

            modelBuilder.Entity<ComplianceRecord>().HasData(
                new ComplianceRecord { Id = 1, EntityId = 1, Type = "Employer", Result = "Compliant", Notes = "Quarterly compliance check passed", Date = new DateTime(2026, 3, 1), OfficerId = 6 },
                new ComplianceRecord { Id = 2, EntityId = 1, Type = "Employer", Result = "Under Review", Notes = "Wage documentation review pending", Date = new DateTime(2026, 3, 15), OfficerId = 6 }
            );

            modelBuilder.Entity<Audit>().HasData(
                new Audit { Id = 1, OfficerId = 7, Scope = "Employer", Findings = "Review of hiring practices and wage compliance", Date = new DateTime(2026, 3, 1), Status = "Open" },
                new Audit { Id = 2, OfficerId = 7, Scope = "Program", Findings = "Evaluation of program effectiveness and budget utilization", Date = new DateTime(2026, 2, 20), Status = "Completed" }
            );
        }
    }
}
