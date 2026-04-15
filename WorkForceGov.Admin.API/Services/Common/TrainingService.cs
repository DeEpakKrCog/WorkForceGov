using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Common
{
    public class TrainingService : ITrainingService
    {
        private readonly ITrainingRepository _trainingRepository;
        private readonly ITrainingEnrollmentRepository _enrollmentRepository;

        public TrainingService(ITrainingRepository trainingRepository, ITrainingEnrollmentRepository enrollmentRepository)
        {
            _trainingRepository = trainingRepository;
            _enrollmentRepository = enrollmentRepository;
        }

        public async Task<IEnumerable<Training>> GetAllAsync() =>
            await _trainingRepository.GetAllAsync();

        public async Task<IEnumerable<Training>> GetAllTrainingsAsync() =>
            await _trainingRepository.GetAllAsync();

        public async Task<Training?> GetByIdAsync(int id) =>
            await _trainingRepository.GetByIdAsync(id);

        public async Task<Training?> GetTrainingByIdAsync(int id) =>
            await _trainingRepository.GetByIdAsync(id);

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
            catch (Exception ex) { return (false, $"Error creating training: {ex.Message}"); }
        }

        public async Task<(bool Success, string Message)> CreateAsync(object model)
        {
            if (model is Training t) return await CreateTrainingAsync(t);
            return (false, "Invalid training data");
        }

        public async Task<(bool Success, string Message)> UpdateTrainingAsync(Training training)
        {
            try
            {
                _trainingRepository.Update(training);
                await _trainingRepository.SaveAsync();
                return (true, "Training updated successfully");
            }
            catch (Exception ex) { return (false, $"Error updating training: {ex.Message}"); }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(object model)
        {
            if (model is Training t) return await UpdateTrainingAsync(t);
            return (false, "Invalid training data");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var training = await _trainingRepository.GetByIdAsync(id);
                if (training == null) return (false, "Training not found");
                _trainingRepository.Remove(training);
                await _trainingRepository.SaveAsync();
                return (true, "Training deleted successfully");
            }
            catch (Exception ex) { return (false, $"Error deleting training: {ex.Message}"); }
        }

        // ── Enrollment ────────────────────────────────────────────────────
        public async Task<IEnumerable<TrainingEnrollment>> GetEnrollmentsByCitizenAsync(int citizenId) =>
            await _enrollmentRepository.GetByCitizenAsync(citizenId);

        public async Task<bool> IsEnrolledAsync(int citizenId, int trainingId) =>
            await _enrollmentRepository.GetByCitizenAndTrainingAsync(citizenId, trainingId) != null;

        public async Task<(bool Success, string Message)> EnrollAsync(int citizenId, int trainingId)
        {
            try
            {
                var existing = await _enrollmentRepository.GetByCitizenAndTrainingAsync(citizenId, trainingId);
                if (existing != null) return (false, "You are already enrolled in this training.");

                var training = await _trainingRepository.GetByIdAsync(trainingId);
                if (training == null) return (false, "Training not found.");
                if (training.Status != "Active") return (false, "This training is not currently active.");

                await _enrollmentRepository.AddAsync(new TrainingEnrollment
                {
                    CitizenId = citizenId,
                    TrainingId = trainingId,
                    EnrolledDate = DateTime.Now,
                    Status = "Enrolled"
                });
                await _enrollmentRepository.SaveAsync();
                return (true, $"Successfully enrolled in \"{training.Title}\".");
            }
            catch (Exception ex) { return (false, $"Enrollment error: {ex.Message}"); }
        }

        public async Task<(bool Success, string Message)> UnenrollAsync(int citizenId, int trainingId)
        {
            try
            {
                var enrollment = await _enrollmentRepository.GetByCitizenAndTrainingAsync(citizenId, trainingId);
                if (enrollment == null) return (false, "You are not enrolled in this training.");
                _enrollmentRepository.Remove(enrollment);
                await _enrollmentRepository.SaveAsync();
                return (true, "You have been unenrolled from the training.");
            }
            catch (Exception ex) { return (false, $"Unenroll error: {ex.Message}"); }
        }
    }
}
