using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_RentAFriend.Models
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        [MaxLength(80)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public bool IsApproved { get; set; } = false;

        [MaxLength(500)]
        public string? ModeratorComment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public object? Tag { get; set; }

        [ForeignKey(nameof(BookingID))]
        public virtual Booking? Booking { get; set; }
    }
}