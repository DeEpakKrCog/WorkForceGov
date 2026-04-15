using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class TrainingEnrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CitizenId { get; set; }
        [ForeignKey("CitizenId")]
        public virtual Citizen Citizen { get; set; } = null!;

        [Required]
        public int TrainingId { get; set; }
        [ForeignKey("TrainingId")]
        public virtual Training Training { get; set; } = null!;

        public DateTime EnrolledDate { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string Status { get; set; } = "Enrolled"; // Enrolled, Completed, Dropped
    }
}
