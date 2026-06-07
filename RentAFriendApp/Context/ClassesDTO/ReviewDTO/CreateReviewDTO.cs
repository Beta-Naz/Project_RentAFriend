using System.ComponentModel.DataAnnotations;

namespace Project_RentAFriend.Models.ClassesDTO.ReviewDTO
{
    public class CreateReviewDTO
    {
        public int BookingID { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}