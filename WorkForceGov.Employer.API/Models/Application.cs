using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class Application
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CitizenId { get; set; }
        [ForeignKey("CitizenId")]
        public virtual Citizen Citizen { get; set; } = null!;

        [Required]
        public int JobOpeningId { get; set; }
        [ForeignKey("JobOpeningId")]
        public virtual JobOpening JobOpening { get; set; } = null!;

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        [Required, StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(2000)]
        public string? CoverLetter { get; set; }

        public DateTime? ReviewedDate { get; set; }

        [StringLength(1000)]
        public string? ReviewNotes { get; set; }

        // ════════ NEW PROPERTY ════════
        // [NotMapped] ensures Entity Framework ignores this when updating the DB.
        // It is strictly used to pass the Resume URL back to your Angular frontend.
        [NotMapped]
        public string? ResumeUrl { get; set; }
    }
}