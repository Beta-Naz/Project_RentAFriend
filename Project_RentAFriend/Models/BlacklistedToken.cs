using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_RentAFriend.Models
{
    [Table("BlacklistedTokens")]
    public class BlacklistedToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        public int? UserID { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;
    }
}
