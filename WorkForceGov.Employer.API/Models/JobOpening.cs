using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // Added for protection

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

        // FK - Keep this required
        [Required]
        public int EmployerId { get; set; }

        // FIX: Make this nullable (?) and use JsonIgnore to prevent 
        // the API from expecting a full object in the JSON payload.
        [ForeignKey("EmployerId")]
        [JsonIgnore]
        public virtual Employer? Employer { get; set; }

        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}