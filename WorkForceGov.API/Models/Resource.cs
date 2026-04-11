using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class Resource
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Type { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Available";

        [Required]
        public int ProgramId { get; set; }
        [ForeignKey("ProgramId")]
        public virtual EmploymentProgram Program { get; set; } = null!;
    }
}
