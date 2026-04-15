using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class Violation
    {
        [Key]
        public int Id { get; set; }

        public int EmployerId { get; set; }
        [ForeignKey("EmployerId")]
        public virtual Employer Employer { get; set; } = null!;

        public int OfficerId { get; set; }
        [ForeignKey("OfficerId")]
        public virtual User Officer { get; set; } = null!;

        [StringLength(100)]
        public string ViolationType { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Penalty { get; set; }

        public DateTime ViolationDate { get; set; } = DateTime.Now;
    }
}
