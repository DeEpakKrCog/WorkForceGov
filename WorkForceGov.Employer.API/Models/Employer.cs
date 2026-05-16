using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // 1. ADD THIS IMPORT
using System.Text.Json.Serialization; // 2. ADD THIS IMPORT

namespace WorkForceGovProject.Models
{
    public class Employer
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Industry { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? ContactInfo { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [ValidateNever] // Backend sets this
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [StringLength(30)]
        [ValidateNever] // Backend sets this
        public string Status { get; set; } = "Pending";

        // FK
        [ValidateNever] // Extracted from JWT, Angular doesn't send it
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [ValidateNever] // Crucial: Tell ASP.NET not to expect a full User object from Angular
        [JsonIgnore]
        public virtual User? User { get; set; }

        // Navigation
        [ValidateNever]
        [JsonIgnore]
        public virtual ICollection<EmployerDocument> Documents { get; set; } = new List<EmployerDocument>();

        [ValidateNever]
        [JsonIgnore]
        public virtual ICollection<JobOpening> JobOpenings { get; set; } = new List<JobOpening>();
    }
}