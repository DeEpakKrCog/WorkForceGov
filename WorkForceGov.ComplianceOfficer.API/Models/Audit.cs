using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class Audit
    {
        [Key]
        public int Id { get; set; }

        public int OfficerId { get; set; }
        [ForeignKey("OfficerId")]
        public virtual User Officer { get; set; } = null!;

        [StringLength(50)]
        public string Scope { get; set; } = string.Empty;

        public string? Findings { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string Status { get; set; } = "Open";
    }
}
