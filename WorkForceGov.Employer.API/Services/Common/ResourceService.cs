using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Common
{
    public class ResourceService : IResourceService
    {
        private readonly IResourceRepository _resourceRepository;

        public ResourceService(IResourceRepository resourceRepository)
        {
            _resourceRepository = resourceRepository;
        }

        public async Task<IEnumerable<Resource>> GetAllResourcesAsync()
        {
            return await _resourceRepository.GetAllAsync();
        }

        public async Task<Resource?> GetResourceByIdAsync(int id)
        {
            return await _resourceRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Resource>> GetResourcesByProgramAsync(int programId)
        {
            var resources = await _resourceRepository.GetAllAsync();
            return resources.Where(r => r.ProgramId == programId).ToList();
        }

        public async Task<(bool Success, string Message)> CreateResourceAsync(Resource resource)
        {
            try
            {
                await _resourceRepository.AddAsync(resource);
                await _resourceRepository.SaveAsync();
                return (true, "Resource created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating resource: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CreateAsync(object model)
        {
            try
            {
                if (model is Resource resource)
                {
                    await _resourceRepository.AddAsync(resource);
                    await _resourceRepository.SaveAsync();
                    return (true, "Resource created successfully");
                }
                return (false, "Invalid resource data");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating resource: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateResourceAsync(Resource resource)
        {
            try
            {
                _resourceRepository.Update(resource);
                await _resourceRepository.SaveAsync();
                return (true, "Resource updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating resource: {ex.Message}");
            }
        }
    }
}
