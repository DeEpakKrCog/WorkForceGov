using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class Benefit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProgramId { get; set; }
        [ForeignKey("ProgramId")]
        public virtual EmploymentProgram Program { get; set; } = null!;

        [Required]
        public int CitizenId { get; set; }
        [ForeignKey("CitizenId")]
        public virtual Citizen Citizen { get; set; } = null!;

        [Required, StringLength(50)]
        public string BenefitType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime BenefitDate { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string Status { get; set; } = "Active";

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
