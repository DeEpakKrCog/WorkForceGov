using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class EmployerDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployerId { get; set; }
        [ForeignKey("EmployerId")]
        public virtual Employer Employer { get; set; } = null!;

        [Required, StringLength(100)]
        public string DocType { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string FileURL { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string VerificationStatus { get; set; } = "Pending";
    }
}
