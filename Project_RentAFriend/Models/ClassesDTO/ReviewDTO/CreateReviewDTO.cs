using System.ComponentModel.DataAnnotations;

namespace Project_RentAFriend.Models.ClassesDTO.ReviewDTO
{
    public class CreateReviewDTO
    {
        [Required]
        public int BookingID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        [MaxLength(100)]
        public string? Title { get; set; }
        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}