using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_RentAFriend.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        public int FriendProfileID { get; set; }

        [Required]
        public int ScheduleID { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime? PaymentDate { get; set; }

        [MaxLength(200)]
        public string? MeetingLocation { get; set; }

        [MaxLength(1000)]
        public string? SpecialRequests { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ClientID))]
        public virtual User? Client { get; set; }

        [ForeignKey(nameof(FriendProfileID))]
        public virtual FriendProfile? FriendProfile { get; set; }

        [ForeignKey(nameof(ScheduleID))]
        public virtual Schedule? Schedule { get; set; }
        public virtual Review? Review { get; set; }

        [NotMapped]
        public object? Tag { get; set; }
    }
}