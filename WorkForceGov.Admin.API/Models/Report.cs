using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string ReportName { get; set; } = string.Empty;

        public int GeneratedBy { get; set; }
        [ForeignKey("GeneratedBy")]
        public virtual User GeneratedByUser { get; set; } = null!;

        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        [Required, StringLength(50)]
        public string ReportType { get; set; } = string.Empty;

        public string? ReportContent { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
