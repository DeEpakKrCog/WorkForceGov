using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Citizen
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _appRepository;
        private readonly INotificationRepository _notifications;
        private readonly ICitizenRepository _citizens;

        public ApplicationService(
            IApplicationRepository appRepository,
            INotificationRepository notifications,
            ICitizenRepository citizens)
        {
            _appRepository = appRepository;
            _notifications = notifications;
            _citizens = citizens;
        }

        public async Task<IEnumerable<Application>> GetApplicationsAsync() =>
            await _appRepository.GetAllAsync();

        public async Task<Application?> GetApplicationByIdAsync(int id) =>
            await _appRepository.GetByIdAsync(id);

        public async Task<IEnumerable<Application>> GetApplicationsByCitizenAsync(int citizenId) =>
            await _appRepository.GetByCitizenAsync(citizenId);

        public async Task<IEnumerable<Application>> GetApplicationsByJobAsync(int jobOpeningId) =>
            await _appRepository.GetByJobAsync(jobOpeningId);

        public async Task<IEnumerable<Application>> GetByJobAsync(int jobOpeningId)
        {
            if (jobOpeningId == 0)
                return await _appRepository.GetAllWithDetailsAsync();
            return await _appRepository.GetByJobAsync(jobOpeningId);
        }

        public async Task<IEnumerable<Application>> GetAllWithDetailsAsync() =>
            await _appRepository.GetAllWithDetailsAsync();

        public async Task<IEnumerable<Application>> GetByEmployerAsync(int employerId) =>
            await _appRepository.GetByEmployerAsync(employerId);

        public async Task<Application?> GetWithDetailsAsync(int id) =>
            await _appRepository.GetWithDetailsAsync(id);

        public async Task<(bool Success, string Message)> UpdateStatusAsync(
            int applicationId, string status, string? notes = null)
        {
            try
            {
                var app = await _appRepository.GetWithDetailsAsync(applicationId);
                if (app == null)
                    return (false, "Application not found");

                app.Status = status;
                app.ReviewNotes = notes;
                app.ReviewedDate = DateTime.Now;
                _appRepository.Update(app);
                await _appRepository.SaveAsync();

                // Send notification to citizen when shortlisted
                if (status == "Shortlisted" && app.Citizen != null)
                {
                    var jobTitle = app.JobOpening?.JobTitle ?? "a position";
                    await _notifications.AddAsync(new Notification
                    {
                        UserId = app.Citizen.UserId,
                        Message = $"Congratulations! You have been shortlisted for '{jobTitle}'. The employer will contact you soon.",
                        Category = "Shortlisted",
                        EntityId = app.Id,
                        EntityType = "Application",
                        CreatedDate = DateTime.Now
                    });
                    await _notifications.SaveAsync();
                }
                else if (status == "Approved" && app.Citizen != null)
                {
                    var jobTitle = app.JobOpening?.JobTitle ?? "a position";
                    await _notifications.AddAsync(new Notification
                    {
                        UserId = app.Citizen.UserId,
                        Message = $"Your application for '{jobTitle}' has been approved! Please check your email for next steps.",
                        Category = "ApplicationApproved",
                        EntityId = app.Id,
                        EntityType = "Application",
                        CreatedDate = DateTime.Now
                    });
                    await _notifications.SaveAsync();
                }
                else if (status == "Rejected" && app.Citizen != null)
                {
                    var jobTitle = app.JobOpening?.JobTitle ?? "a position";
                    await _notifications.AddAsync(new Notification
                    {
                        UserId = app.Citizen.UserId,
                        Message = $"Your application for '{jobTitle}' was not selected at this time. Keep applying!",
                        Category = "ApplicationRejected",
                        EntityId = app.Id,
                        EntityType = "Application",
                        CreatedDate = DateTime.Now
                    });
                    await _notifications.SaveAsync();
                }

                return (true, $"Application status updated to '{status}' successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating application: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ApplyAsync(int citizenId, int jobOpeningId)
        {
            try
            {
                var app = new Application
                {
                    CitizenId = citizenId,
                    JobOpeningId = jobOpeningId,
                    Status = "Pending",
                    SubmittedDate = DateTime.Now
                };
                await _appRepository.AddAsync(app);
                await _appRepository.SaveAsync();
                return (true, "Application submitted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error submitting application: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ApplyAsync(
            int citizenId, int jobOpeningId, string? coverLetter)
        {
            try
            {
                var app = new Application
                {
                    CitizenId = citizenId,
                    JobOpeningId = jobOpeningId,
                    Status = "Pending",
                    SubmittedDate = DateTime.Now,
                    CoverLetter = coverLetter
                };
                await _appRepository.AddAsync(app);
                await _appRepository.SaveAsync();
                return (true, "Application submitted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error submitting application: {ex.Message}");
            }
        }

        public async Task<IEnumerable<Application>> GetByCitizenAsync(int citizenId) =>
            await GetApplicationsByCitizenAsync(citizenId);

        public async Task<(bool Success, string Message)> WithdrawAsync(int applicationId)
        {
            try
            {
                var app = await _appRepository.GetByIdAsync(applicationId);
                if (app == null)
                    return (false, "Application not found");

                _appRepository.Remove(app);
                await _appRepository.SaveAsync();
                return (true, "Application withdrawn successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error withdrawing application: {ex.Message}");
            }
        }
    }
}
