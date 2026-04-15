using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class CitizenDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CitizenId { get; set; }
        [ForeignKey("CitizenId")]
        public virtual Citizen Citizen { get; set; } = null!;

        [Required, StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? FilePath { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [Required, StringLength(30)]
        public string VerificationStatus { get; set; } = "Pending";

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime? VerificationDate { get; set; }

        public int? VerifiedByUserId { get; set; }
        [ForeignKey("VerifiedByUserId")]
        public virtual User? VerifiedBy { get; set; }
    }
}
