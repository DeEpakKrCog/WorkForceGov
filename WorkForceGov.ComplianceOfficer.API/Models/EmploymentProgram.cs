using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class EmploymentProgram
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string ProgramName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(50)]
        public string? ProgramType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalBudget { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Active";

        // Navigation
        public virtual ICollection<Benefit> Benefits { get; set; } = new List<Benefit>();
        public virtual ICollection<Training> Trainings { get; set; } = new List<Training>();
        public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
    }
}
