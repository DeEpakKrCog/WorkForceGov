using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Models.ViewModels;

namespace WorkForceGovProject.Services.Common
{
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _users;
        public AccountService(IUserRepository users) { _users = users; }

        public async Task<(bool, string, User?)> LoginAsync(string email, string password)
        {
            var user = await _users.GetByEmailAsync(email);
            if (user == null) return (false, "Invalid email or password.", null);
            if (user.Password != password) return (false, "Invalid email or password.", null);
            if (user.Status != "Active") return (false, "Account is inactive.", null);
            return (true, "Login successful.", user);
        }

        public async Task<(bool, string)> RegisterAsync(
            string fullName, string email, string password, string role, string? phone)
        {
            if (await _users.AnyAsync(u => u.Email == email))
                return (false, "Email already registered.");

            await _users.AddAsync(new User
            {
                FullName = fullName, Email = email, Password = password,
                Role = role, Phone = phone
            });
            await _users.SaveAsync();
            return (true, "Registration successful.");
        }

        public async Task<(bool, string)> RegisterAsync(RegisterViewModel model)
        {
            if (await _users.AnyAsync(u => u.Email == model.Email))
                return (false, "Email already registered.");

            if (model.Password != model.ConfirmPassword)
                return (false, "Passwords do not match.");

            await _users.AddAsync(new User
            {
                FullName = model.FullName, Email = model.Email, Password = model.Password,
                Role = model.Role, Phone = model.Phone
            });
            await _users.SaveAsync();
            return (true, "Registration successful.");
        }

        public async Task<User?> GetByIdAsync(int id) => await _users.GetByIdAsync(id);
        public async Task<IEnumerable<User>> GetAllUsersAsync() => await _users.GetAllAsync();

        public async Task<(bool, string)> CreateUserAsync(
            string fullName, string email, string password, string role, string? phone)
        {
            if (await _users.AnyAsync(u => u.Email == email))
                return (false, "Email already exists.");
            await _users.AddAsync(new User
            {
                FullName = fullName, Email = email, Password = password,
                Role = role, Phone = phone
            });
            await _users.SaveAsync();
            return (true, "User created.");
        }

        public async Task<(bool, string)> CreateUserAsync(CreateUserViewModel model)
        {
            if (await _users.AnyAsync(u => u.Email == model.Email))
                return (false, "Email already exists.");
            await _users.AddAsync(new User
            {
                FullName = model.FullName, Email = model.Email, Password = model.Password,
                Role = model.Role, Phone = model.Phone
            });
            await _users.SaveAsync();
            return (true, "User created.");
        }

        public async Task<(bool, string)> UpdateUserAsync(User user)
        {
            _users.Update(user);
            await _users.SaveAsync();
            return (true, "User updated.");
        }

        public async Task<(bool, string)> DeactivateUserAsync(int id)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null) return (false, "User not found.");
            user.Status = "Inactive";
            _users.Update(user);
            await _users.SaveAsync();
            return (true, "User deactivated.");
        }

        public async Task<(bool, string)> DeleteUserAsync(int id)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null) return (false, "User not found.");
            _users.Remove(user);
            await _users.SaveAsync();
            return (true, "User deleted.");
        }
    }

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notifications;
        public NotificationService(INotificationRepository n) { _notifications = n; }

        public async Task<IEnumerable<Notification>> GetByUserAsync(int userId) =>
            await _notifications.GetByUserAsync(userId);
        public async Task<int> GetUnreadCountAsync(int userId) =>
            await _notifications.GetUnreadCountAsync(userId);
        public async Task CreateAsync(int userId, string message, string category,
            int? entityId = null, string? entityType = null)
        {
            await _notifications.AddAsync(new Notification
            {
                UserId = userId, Message = message, Category = category,
                EntityId = entityId, EntityType = entityType
            });
            await _notifications.SaveAsync();
        }
        public async Task MarkAllReadAsync(int userId) =>
            await _notifications.MarkAllReadAsync(userId);
    }

    public class SystemLogService : ISystemLogService
    {
        private readonly ISystemLogRepository _logs;
        public SystemLogService(ISystemLogRepository l) { _logs = l; }

        public async Task LogAsync(int userId, string action, string? resource = null, string? ip = null)
        {
            await _logs.AddAsync(new SystemLog
            {
                UserId = userId, Action = action, Resource = resource, IpAddress = ip
            });
            await _logs.SaveAsync();
        }

        public async Task<IEnumerable<SystemLog>> GetRecentAsync(int count = 50) =>
            (await _logs.GetAllAsync()).OrderByDescending(l => l.Timestamp).Take(count);
    }
}
