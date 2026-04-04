using WorkForceGovProject.Interfaces.Repositories;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Services.Citizen
{
    public class DocumentService : IDocumentService
    {
        private readonly ICitizenDocumentRepository _citizenDocRepository;
        private readonly IEmployerDocumentRepository _employerDocRepository;

        public DocumentService(ICitizenDocumentRepository citizenDocRepository, IEmployerDocumentRepository employerDocRepository)
        {
            _citizenDocRepository = citizenDocRepository;
            _employerDocRepository = employerDocRepository;
        }

        public async Task<IEnumerable<CitizenDocument>> GetCitizenDocumentsAsync(int citizenId)
        {
            var docs = await _citizenDocRepository.GetAllAsync();
            return docs.Where(d => d.CitizenId == citizenId).ToList();
        }

        public async Task<IEnumerable<EmployerDocument>> GetEmployerDocumentsAsync(int employerId)
        {
            var docs = await _employerDocRepository.GetAllAsync();
            return docs.Where(d => d.EmployerId == employerId).ToList();
        }

        public async Task<CitizenDocument?> GetCitizenDocumentByIdAsync(int id)
        {
            return await _citizenDocRepository.GetByIdAsync(id);
        }

        public async Task<EmployerDocument?> GetEmployerDocumentByIdAsync(int id)
        {
            return await _employerDocRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<CitizenDocument>> GetPendingAsync()
        {
            var docs = await _citizenDocRepository.GetAllAsync();
            return docs.Where(d => d.VerificationStatus == "Pending").ToList();
        }

        public async Task<(bool Success, string Message)> VerifyAsync(int documentId, int verifiedByUserId)
        {
            try
            {
                var doc = await _citizenDocRepository.GetByIdAsync(documentId);
                if (doc == null)
                    return (false, "Document not found");

                doc.VerificationStatus = "Verified";
                doc.VerifiedByUserId = verifiedByUserId;
                _citizenDocRepository.Update(doc);
                await _citizenDocRepository.SaveAsync();
                return (true, "Document verified successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error verifying document: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectAsync(int documentId, int rejectedByUserId, string reason)
        {
            try
            {
                var doc = await _citizenDocRepository.GetByIdAsync(documentId);
                if (doc == null)
                    return (false, "Document not found");

                doc.VerificationStatus = "Rejected";
                doc.VerifiedByUserId = rejectedByUserId;
                _citizenDocRepository.Update(doc);
                await _citizenDocRepository.SaveAsync();
                return (true, "Document rejected successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error rejecting document: {ex.Message}");
            }
        }

        public async Task<IEnumerable<CitizenDocument>> GetByCitizenAsync(int citizenId)
        {
            return await GetCitizenDocumentsAsync(citizenId);
        }

        public async Task<(bool Success, string Message)> UploadAsync(int citizenId, string documentType, string filePath)
        {
            try
            {
                var doc = new CitizenDocument
                {
                    CitizenId = citizenId,
                    DocumentType = documentType,
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    UploadedDate = DateTime.Now,
                    VerificationStatus = "Pending"
                };
                await _citizenDocRepository.AddAsync(doc);
                await _citizenDocRepository.SaveAsync();
                return (true, "Document uploaded successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error uploading document: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UploadAsync(int citizenId, string documentType, string fileName, string filePath)
        {
            try
            {
                var doc = new CitizenDocument
                {
                    CitizenId = citizenId,
                    DocumentType = documentType,
                    FilePath = filePath,
                    FileName = fileName,
                    UploadedDate = DateTime.Now,
                    VerificationStatus = "Pending"
                };
                await _citizenDocRepository.AddAsync(doc);
                await _citizenDocRepository.SaveAsync();
                return (true, "Document uploaded successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error uploading document: {ex.Message}");
            }
        }
    }
}
