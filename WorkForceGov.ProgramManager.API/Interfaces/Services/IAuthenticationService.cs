using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> AuthenticateAsync(string email, string password);
    }
}
