using Microsoft.EntityFrameworkCore;
using WorkForceGovProject.Data;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Common
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;

        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LoginResponse> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Email and password are required."
                };
            }

            try
            {
                // Use async query with proper async method
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                // Compare passwords - using simple string comparison (NOT recommended for production)
                // In production, use BCrypt or similar: BCrypt.Net.BCrypt.Verify(password, user.Password)
                if (user.Password != password)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                return new LoginResponse
                {
                    Success = true,
                    Message = "Authentication successful.",
                    UserId = user.Id,
                    UserRole = user.Role
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Authentication error: {ex.Message}"
                };
            }
        }
    }
}
