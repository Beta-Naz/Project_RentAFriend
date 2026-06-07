using System.ComponentModel.DataAnnotations;

namespace Project_RentAFriend.Models.ClassesDTO.BookingDTO
{
    public class CreateBookingDTO
    {
        [Required]
        public int ScheduleID { get; set; }

        [Required]
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? MeetingLocation { get; set; }

        [MaxLength(1000)]
        public string? SpecialRequests { get; set; }
    }
}