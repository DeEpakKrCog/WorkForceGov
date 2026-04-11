using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IResourceService
    {
        Task<IEnumerable<Resource>> GetAllResourcesAsync();
        Task<Resource?> GetResourceByIdAsync(int id);
        Task<IEnumerable<Resource>> GetResourcesByProgramAsync(int programId);
        Task<(bool Success, string Message)> CreateResourceAsync(Resource resource);
        Task<(bool Success, string Message)> CreateAsync(object model);
        Task<(bool Success, string Message)> UpdateResourceAsync(Resource resource);
    }
}
