namespace WorkForceGovProject.Models.ViewModels
{
    public class DocumentUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string DocumentType { get; set; } = null!;
    }
}