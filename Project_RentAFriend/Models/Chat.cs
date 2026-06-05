using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_RentAFriend.Models
{
    public class Chat
    {
        [Key]
        public int ChatID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        public int FriendID { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastMessageAt { get; set; }

        [ForeignKey(nameof(ClientID))]
        public virtual User? Client { get; set; }

        [ForeignKey(nameof(FriendID))]
        public virtual User? Friend { get; set; }
        public virtual ICollection<Message>? Messages { get; set; }
    }
}
