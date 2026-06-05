using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_RentAFriend.Models
{
    public class FriendProfile
    {
        [Key]
        public int ProfileID { get; set; }

        [Required]
        public int UserID { get; set; }

        [MaxLength(1000)]
        public string? Bio { get; set; }

        [Range(18, 99)]
        public int? Age { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(500)]
        public string? Hobbies { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? HourlyRate { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? AverageRating { get; set; }

        public bool IsVerified { get; set; } = false;

        [MaxLength(500)]
        public string? VerificationNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserID))]
        public virtual User? User { get; set; }
        public virtual ICollection<Schedule>? Schedules { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; }
    }
}