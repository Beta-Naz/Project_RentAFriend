using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_RentAFriend.Models
{
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }
        public int? UserID { get; set; }
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string TableName { get; set; } = string.Empty;
        public int RecordID { get; set; }
        [MaxLength(4000)]
        public string? OldValue { get; set; }
        [MaxLength(4000)]
        public string? NewValue { get; set; }
        [MaxLength(45)]
        public string? IPAddress { get; set; }
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey(nameof(UserID))]
        public virtual User? User { get; set; }
    }
}
