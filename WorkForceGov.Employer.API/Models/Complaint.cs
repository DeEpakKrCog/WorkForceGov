using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class Complaint
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public int EmployerId { get; set; }
        [ForeignKey("EmployerId")]
        public virtual Employer Employer { get; set; } = null!;

        public string ComplaintDescription { get; set; } = string.Empty;

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime SubmittedDate { get; set; } = DateTime.Now;
    }
}
