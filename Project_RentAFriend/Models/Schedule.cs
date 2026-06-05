using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_RentAFriend.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleID { get; set; }

        [Required]
        public int ProfileID { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public int? BookingID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ProfileID))]
        public virtual FriendProfile? FriendProfile { get; set; }

        [ForeignKey(nameof(BookingID))]
        public virtual Booking? Booking { get; set; }
    }
}