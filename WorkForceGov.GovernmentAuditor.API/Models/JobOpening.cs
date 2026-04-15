using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class JobOpening
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string JobTitle { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(100)]
        public string? JobCategory { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryMin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryMax { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.Now;
        public DateTime? ClosingDate { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Open";

        // FK
        [Required]
        public int EmployerId { get; set; }
        [ForeignKey("EmployerId")]
        public virtual Employer Employer { get; set; } = null!;

        // Navigation
        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
