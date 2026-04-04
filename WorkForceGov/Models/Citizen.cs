using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class Citizen
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DOB { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(30)]
        public string DocumentStatus { get; set; } = "Pending";

        // FK
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        // Navigation
        public virtual ICollection<CitizenDocument> Documents { get; set; } = new List<CitizenDocument>();
        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
        public virtual ICollection<Benefit> Benefits { get; set; } = new List<Benefit>();
    }
}
