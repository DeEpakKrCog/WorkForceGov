using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WorkForceGovProject.Models
{
    public class Training
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Active";

        [Required]
        public int ProgramId { get; set; }

        // 🚨 FIX 1: Add the '?' right after EmploymentProgram
        [ForeignKey("ProgramId")]
        public virtual EmploymentProgram? Program { get; set; }

        // 🚨 FIX 2: Add [JsonIgnore] or just ignore validation so it doesn't fail if the array is missing
        [JsonIgnore]
        public virtual ICollection<TrainingEnrollment> Enrollments { get; set; } = new List<TrainingEnrollment>();
    }
}