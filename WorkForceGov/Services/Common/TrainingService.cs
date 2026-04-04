using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Common
{
    public class TrainingService : ITrainingService
    {
        private readonly ITrainingRepository _trainingRepository;

        public TrainingService(ITrainingRepository trainingRepository)
        {
            _trainingRepository = trainingRepository;
        }

        public async Task<IEnumerable<Training>> GetAllAsync()
        {
            return await _trainingRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Training>> GetAllTrainingsAsync()
        {
            return await _trainingRepository.GetAllAsync();
        }

        public async Task<Training?> GetByIdAsync(int id)
        {
            return await _trainingRepository.GetByIdAsync(id);
        }

        public async Task<Training?> GetTrainingByIdAsync(int id)
        {
            return await _trainingRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Training>> GetTrainingsByProgramAsync(int programId)
        {
            var trainings = await _trainingRepository.GetAllAsync();
            return trainings.Where(t => t.ProgramId == programId).ToList();
        }

        public async Task<(bool Success, string Message)> CreateTrainingAsync(Training training)
        {
            try
            {
                await _trainingRepository.AddAsync(training);
                await _trainingRepository.SaveAsync();
                return (true, "Training created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating training: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CreateAsync(object model)
        {
            try
            {
                if (model is Training training)
                {
                    await _trainingRepository.AddAsync(training);
                    await _trainingRepository.SaveAsync();
                    return (true, "Training created successfully");
                }
                return (false, "Invalid training data");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating training: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateTrainingAsync(Training training)
        {
            try
            {
                _trainingRepository.Update(training);
                await _trainingRepository.SaveAsync();
                return (true, "Training updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating training: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(object model)
        {
            try
            {
                if (model is Training training)
                {
                    _trainingRepository.Update(training);
                    await _trainingRepository.SaveAsync();
                    return (true, "Training updated successfully");
                }
                return (false, "Invalid training data");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating training: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var training = await _trainingRepository.GetByIdAsync(id);
                if (training == null)
                    return (false, "Training not found");

                _trainingRepository.Remove(training);
                await _trainingRepository.SaveAsync();
                return (true, "Training deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting training: {ex.Message}");
            }
        }
    }
}
