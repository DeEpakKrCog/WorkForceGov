using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IAccountService
    {
        Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password);
        Task<(bool Success, string Message)> RegisterAsync(string fullName, string email, string password, string role, string? phone);
        Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model);
        Task<User?> GetByIdAsync(int id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<(bool Success, string Message)> CreateUserAsync(CreateUserViewModel model);
        Task<(bool Success, string Message)> UpdateUserAsync(User user);
        Task<(bool Success, string Message)> DeactivateUserAsync(int id);
        Task<(bool Success, string Message)> DeleteUserAsync(int id);
    }

    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetByUserAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task CreateAsync(int userId, string message, string category, int? entityId = null, string? entityType = null);
        Task MarkAllReadAsync(int userId);
    }

    public interface ISystemLogService
    {
        Task LogAsync(int userId, string action, string? resource = null, string? ip = null);
        Task<IEnumerable<SystemLog>> GetRecentAsync(int count = 50);
    }
}
