using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class ComplianceRecord
    {
        [Key]
        public int Id { get; set; }

        public int EntityId { get; set; }

        [Required, StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [StringLength(30)]
        public string Result { get; set; } = "Under Review";

        public DateTime Date { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        public int? OfficerId { get; set; }
        [ForeignKey("OfficerId")]
        public virtual User? Officer { get; set; }
    }
}
