using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Role { get; set; } = "Citizen";

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Citizen? Citizen { get; set; }
        public virtual Employer? Employer { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();
        public virtual ICollection<Audit> Audits { get; set; } = new List<Audit>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
