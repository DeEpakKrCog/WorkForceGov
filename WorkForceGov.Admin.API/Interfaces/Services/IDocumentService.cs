using WorkForceGovProject.Models;

namespace WorkForceGovProject.Interfaces.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<CitizenDocument>> GetCitizenDocumentsAsync(int citizenId);
        Task<IEnumerable<EmployerDocument>> GetEmployerDocumentsAsync(int employerId);
        Task<CitizenDocument?> GetCitizenDocumentByIdAsync(int id);
        Task<EmployerDocument?> GetEmployerDocumentByIdAsync(int id);
        Task<IEnumerable<CitizenDocument>> GetPendingAsync();
        Task<(bool Success, string Message)> VerifyAsync(int documentId, int verifiedByUserId);
        Task<(bool Success, string Message)> RejectAsync(int documentId, int rejectedByUserId, string reason);
        Task<IEnumerable<CitizenDocument>> GetByCitizenAsync(int citizenId);
        Task<(bool Success, string Message)> UploadAsync(int citizenId, string documentType, string filePath);
        Task<(bool Success, string Message)> UploadAsync(int citizenId, string documentType, string fileName, string filePath);
    }
}
