using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkForceGovProject.Models
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [StringLength(255)]
        public string Action { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Resource { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}
